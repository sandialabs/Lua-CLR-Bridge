/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Lua;
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LuaBridgeTests : SandboxTestsBase
    {
        [TestMethod]
        public void BridgeAlreadyDisposed()
        {
            var lua = CreateLuaBridge();

            lua.Dispose();

            try
            {
                lua.Do("");

                Assert.Fail();
            }
            catch (ObjectDisposedException)
            {
                // expected
            }
        }

        [TestMethod]
        public void LoadStream()
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes("return 'test'")))
            using (var lua = CreateLuaBridge())
            {
                var f = lua.Load(stream);
                
                var r = f.Call();

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(string));
                Assert.AreEqual("test", r[0] as string);
            }
        }

        [TestMethod]
        public void DumpFunction()
        {
            using (var stream = new MemoryStream())
            {
                using (var lua = CreateLuaBridge())
                {
                    var f = lua.Load("return 'test'");

                    f.Dump(stream);
                }

                stream.Seek(0, SeekOrigin.Begin);

                using (var lua = CreateLuaBridge())
                {
                    var f = lua.Load(stream);

                    var r = f.Call();

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(string));
                    Assert.AreEqual("test", r[0] as string);
                }
            }
        }

        [TestMethod]
        public void LoadLib()
        {
            using (var lua = CreateLuaBridge())
            {
                Assert.IsNotNull(lua.Environment);
                Assert.IsNull(lua.Environment["math"]);

                lua.LoadLib("math");

                Assert.IsInstanceOfType(lua.Environment["math"], typeof(LuaTable));
            }
        }

        [TestMethod]
        public void GetSetEnvironment()
        {
            using (var lua = CreateLuaBridge())
            {
                Assert.IsNull(lua["b"]);

                LuaTable environment = lua.Environment;
                environment["b"] = true;

                Assert.AreEqual(true, lua["b"]);

                LuaTable newEnvironment = lua.NewTable();
                newEnvironment["b"] = false;

                lua.Environment = newEnvironment;

                Assert.AreEqual(false, lua["b"]);
            }
        }

        [TestMethod]
        public void SetGetGlobal()
        {
            using (var lua = CreateLuaBridge())
            {
                Assert.IsNull(lua["b"]);

                lua["b"] = true;
                object r = lua["b"];

                Assert.IsInstanceOfType(r, typeof(bool));
                Assert.AreEqual(true, r);
            }
        }

        [TestMethod]
        public void StringTranslationBinarySafe()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("string");

                for (char c = '\x00'; c <= '\xff'; ++c)
                {
                    string s = new string(new char[] { c });

                    lua["s"] = s;

                    var r = lua.Do("return s:len(), s:byte()");

                    Assert.AreEqual(2, r.Length);
                    Assert.AreEqual(1.0, r[0]);
                    Assert.AreEqual((double)c, r[1]);

                    s = lua["s"] as string;
                    
                    Assert.IsNotNull(s);
                    Assert.AreEqual(1, s.Length);
                    Assert.AreEqual(c, s[0]);
                }
            }
        }

        [TestMethod]
        public void ExceptionStackTrace()
        {
            using (var lua = CreateLuaBridge())
            {
                try
                {
                    lua.Do("CLR.Static['System.Environment'].CurrentDirectory = ''");
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(!ex.StackTrace.Contains("stack traceback:"));
                    Assert.IsTrue(ex.StackTrace.Contains(@"[string ""<string>""]:1: in main chunk"));
                }
            }
        }

        [TestMethod]
        public void StackOverflow1()
        {
            using (var lua = new LuaBridge())
            {
                var f = lua.NewFunction(( L ) =>
                    {
                        try
                        {
                            while (true)
                                new PrivateObject(lua).Invoke("PushObject", new object[] { L, null });
                        }
                        catch
                        {
                            LuaWrapper.lua_settop(L, 0);

                            throw;
                        }
                    });

                try
                {
                    f.Call();

                    Assert.Fail();
                }
                catch (LuaRuntimeException ex)
                {
                    Assert.AreEqual("Insufficient stack", ex.Message);
                }
            }
        }

        [TestMethod]
        public void StackOverflow2()
        {
            using (var lua = new LuaBridge())
            {
                try
                {
                    lua.Do("function f() f() end; f()");

                    Assert.Fail();
                }
                catch (LuaRuntimeException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("stack overflow"), ex.Message);
                }
            }
        }


        [TestMethod]
        public void IntsrumentedBridgeMonitorMemory()
        {
            InstrumentedLuaBridge lua;

            using (lua = CreateInstrumentedLuaBridge(Instrumentations.MemoryMonitoring))
            {
                var preTableAllocated = lua.MemoryAllocatedSize;
                Assert.IsTrue((ulong)preTableAllocated > 0);

                var _ = lua.NewTable();

                var postTableAllocated = lua.MemoryAllocatedSize;
                Assert.IsTrue((ulong)preTableAllocated < (ulong)postTableAllocated);
            }

            Assert.IsTrue(lua.MemoryAllocatedSize == UIntPtr.Zero);
        }

        [TestMethod]
        public void InstrumentedBridgeCancel()
        {
            using (var lua = CreateInstrumentedLuaBridge(Instrumentations.Interruption))
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);

                    lua.Cancel("abort!");
                });

                lua.LoadLib("os");

                lua.Do("spin = function( timespan ) start = os.clock() while os.clock() - start < timespan do end end");

                try
                {
                    lua.LoadLib("_G");

                    lua.Do("pcall(spin, 5)");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaRuntimeException));
                    Assert.IsTrue(ex.Message.EndsWith("abort!"));
                }
            }
        }

        [TestMethod]
        public void InstrumentedBridgeInterject()
        {
            using (var lua = CreateInstrumentedLuaBridge(Instrumentations.Interruption))
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);

                    lua.Interject(( bridge ) => bridge.Do("r = 'pass!'"));
                });

                lua.Do("r = 'fail!'");

                lua.LoadLib("os");

                lua.Do("spinret = function( timespan ) start = os.clock() while os.clock() - start < timespan do if r == 'pass!' then break end end return r end");

                var r = lua.Do("return spinret(5)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(string));
                Assert.AreEqual("pass!", r[0] as string);
            }
        }
    }
}
