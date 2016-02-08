/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Test.ObjectTranslator
{
    using System;
    using System.Threading;
    using Lua;
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicTests : SandboxTestsBase
    {
        [TestMethod]
        public void TranslateNil()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["x"] = null;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("nil", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);
            }
        }

        [TestMethod]
        public void TranslateBoolean()
        {
            using (var lua = CreateLuaBridge())
            {
                bool x = true;

                lua["x"] = x;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("boolean", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(x, r[0]);
            }
        }

        public static int IsLightUserData( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_GLOBALS);
            LuaWrapper.lua_gettable(L, LuaWrapper.LUA_REGISTRYINDEX);
            LuaWrapper.lua_insert(L, -2);
            LuaWrapper.lua_gettable(L, -2);
            bool result = LuaWrapper.lua_islightuserdata(L, -1);
            LuaWrapper.lua_settop(L, 0);
            LuaWrapper.lua_pushboolean(L, result);
            return 1;
        }

        [TestMethod]
        public void TranslateLightUserData()
        {
            using (var lua = new LuaBridge())
            {
                lua["x"] = IntPtr.Zero;

                var isLightUserData = lua.NewFunction(IsLightUserData).CallExpectingResults(1, "x")[0] as bool?;

                Assert.IsTrue(isLightUserData ?? false);

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(IntPtr));
                Assert.AreEqual(IntPtr.Zero, r[0]);
            }
        }

        [TestMethod]
        public void TranslateNumber()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");

                var xs = new object[]
                    {
                        Double.Epsilon, Double.MaxValue, Double.MinValue, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity,
                        Single.Epsilon, Single.MaxValue, Single.MinValue, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity,
                        Int32.MaxValue, Int32.MinValue, UInt32.MaxValue, UInt32.MinValue,
                        Int16.MaxValue, Int16.MinValue, UInt16.MaxValue, UInt16.MinValue,
                        SByte.MaxValue, SByte.MinValue, Byte.MaxValue, Byte.MinValue,
                        Char.MaxValue, Char.MinValue,
                    };

                foreach (object x in xs)
                {
                    lua["x"] = x;

                    var r = lua.Do("return type(x)");

                    Assert.AreEqual(1, r.Length);
                    Assert.AreEqual("number", r[0]);

                    r = lua.Do("return x");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    if (x is char)
                        Assert.AreEqual((double)(char)x, r[0]);
                    else
                        Assert.AreEqual(Convert.ToDouble(x), r[0]);
                }

                // 64-bit integers get special treatment
                xs = new object[]
                    {
                        Int64.MaxValue, Int64.MinValue, UInt64.MaxValue, UInt64.MinValue, 
                    };

                foreach (object x in xs)
                {
                    lua["x"] = x;

                    lua.LoadLib("_G");

                    var r = lua.Do("return type(x)");

                    Assert.AreEqual(1, r.Length);
                    Assert.AreNotEqual("number", r[0]);

                    r = lua.Do("return x");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], x.GetType());
                    Assert.AreEqual(x, r[0]);
                }
            }
        }

        [TestMethod]
        public void TranslateString()
        {
            using (var lua = CreateLuaBridge())
            {
                string x = "abc";

                lua["x"] = x;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("string", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(x, r[0]);
            }
        }

        [TestMethod]
        public void TranslateTable()
        {
            using (var lua = CreateLuaBridge())
            {
                LuaTable x = lua.NewTable();

                lua["x"] = x;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("table", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(x, r[0]);
            }
        }

        [TestMethod]
        public void TranslateFunction()
        {
            using (var lua = CreateLuaBridge())
            {
                LuaFunction x = lua.Do("return function () end")[0] as LuaFunction;

                lua["x"] = x;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("function", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(x, r[0]);
            }
        }

        public static int IsCFunction( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_GLOBALS);
            LuaWrapper.lua_gettable(L, LuaWrapper.LUA_REGISTRYINDEX);
            LuaWrapper.lua_insert(L, -2);
            LuaWrapper.lua_gettable(L, -2);
            bool result = LuaWrapper.lua_iscfunction(L, -1);
            LuaWrapper.lua_settop(L, 0);
            LuaWrapper.lua_pushboolean(L, result);
            return 1;
        }

        public static int CFunction( IntPtr L )
        {
            return 0;
        }

        [TestMethod]
        public void TranslateCFunction()
        {
            using (var lua = new LuaBridge())
            {
                LuaCFunction x = CFunction;

                lua["x"] = x;

                var isCFunction = lua.NewFunction(IsCFunction).CallExpectingResults(1, "x")[0] as bool?;

                Assert.IsTrue(isCFunction ?? false);

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaFunction));
                Assert.AreEqual(x, (r[0] as LuaFunction).AsDelegate());

                r = lua.Do("return x()");

                Assert.AreEqual(0, r.Length);
            }
        }

        public static object[] SafeCFunction( LuaBridgeBase bridge, object[] args )
        {
            return new object[] { args.Length };
        }

        [TestMethod]
        public void TranslateSafeCFunction()
        {
            using (var lua = new LuaBridge())
            {
                LuaSafeCFunction x = SafeCFunction;

                lua["x"] = x;

                var isCFunction = lua.NewFunction(IsCFunction).CallExpectingResults(1, "x")[0] as bool?;

                Assert.IsTrue(isCFunction ?? false);

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaFunction));
                Assert.AreEqual(x, (r[0] as LuaFunction).AsDelegate());

                r = lua.Do("return x(1, 2, 3)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)3, r[0]);
            }
        }

        public static int NewUserData( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_GLOBALS);
            LuaWrapper.lua_gettable(L, LuaWrapper.LUA_REGISTRYINDEX);
            LuaWrapper.lua_insert(L, -2);
            LuaWrapper.lua_newuserdata(L, 0);
            LuaWrapper.lua_settable(L, -3);
            LuaWrapper.lua_settop(L, 0);
            return 0;
        }

        [TestMethod]
        public void TranslateUserData()
        {
            using (var lua = new LuaBridge())
            {
                lua.NewFunction(NewUserData).Call("x");

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaUserData));

                lua["y"] = r[0];

                r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }

        [TestMethod]
        public void TranslateObjectUserData()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new Tuple<bool, string>(true, "test");

                lua["x"] = x;

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(r[0], x);
            }
        }

        [TestMethod]
        public void TranslateDelegateUserData()
        {
            using (var lua = CreateLuaBridge())
            {
                Func<object> x = delegate { return null; };

                lua["x"] = x;

                var r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(r[0], x);
            }
        }

        [TestMethod]
        public void TranslateThread()
        {
            using (var lua = CreateLuaBridge())
            {
                LuaThread x = lua.NewThread();

                lua["x"] = x;

                lua.LoadLib("_G");

                var r = lua.Do("return type(x)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("thread", r[0]);

                r = lua.Do("return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], x.GetType());
                Assert.AreEqual(x, r[0]);
            }
        }

        [TestMethod]
        public void MaintainObjectIdentity()
        {
            using (var lua = new LuaBridge())
            {
                var o = new Tuple<bool, string>(true, "test");

                lua["o1"] = o;
                lua["o2"] = o;

                Assert.AreSame(o, lua["o1"]);
                Assert.AreSame(o, lua["o2"]);

                var r = lua.Do("t = {} t[o1] = 1 t[o2] = 2 return t[o1]");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)2, r[0]);
            }
        }

        [Serializable]
        private class DelegateField
        {
            public Func<int, int> y = null;

            public int f( int i ) { return y(i); }
        }

        [TestMethod]
        public void MaintainLuaFunctionDelegateIdentity()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["Delegate"] = new CLRStaticContext(typeof(Delegate));
                lua["Func"] = typeof(Func<int, int>);

                lua["x"] = new DelegateField();

                lua.Do("f = function( i ) return i + 1 end");
                
                lua.Do("x.y = Delegate.Combine(x.y, CLR.NewDelegate(Func, f))");

                var r = lua.Do("return x.f(0)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)1, r[0]);

                lua.Do("g = function( i ) return i + 2 end");

                lua.Do("x.y = Delegate.Combine(x.y, CLR.NewDelegate(Func, g))");

                r = lua.Do("return x.f(0)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)2, r[0]);

                lua.Do("x.y = Delegate.Remove(x.y, CLR.NewDelegate(Func, g))");

                r = lua.Do("return x.f(0)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)1, r[0]);
            }
        }

        [TestMethod]
        public void PreserveObjectIdentity()
        {
            using (var lua = new LuaBridge())
            {
                var o = new Tuple<bool, string>(true, "test");

                lua["o"] = o;

                Assert.AreSame(o, lua["o"]);
            }
        }

        [TestMethod]
        public void PreserveCFunctionDelegateIdentity()
        {
            using (var lua = new LuaBridge())
            {
                LuaCFunction f = delegate( IntPtr L )
                    {
                        LuaWrapper.lua_settop(L, 0);
                        LuaWrapper.lua_pushboolean(L, true);
                        return 1;
                    };

                lua["f"] = f;

                Assert.IsInstanceOfType(lua["f"], typeof(LuaFunction));
                Assert.AreSame(f, (lua["f"] as LuaFunction).AsDelegate());
            }
        }

        [TestMethod]
        public void PreserveSafeCFunctionDelegateIdentity()
        {
            using (var lua = new LuaBridge())
            {
                LuaSafeCFunction f = delegate( LuaBridgeBase bridge, object[] args )
                {
                    return new object[] { true };
                };

                lua["f"] = f;

                Assert.IsInstanceOfType(lua["f"], typeof(LuaFunction));
                Assert.AreSame(f, (lua["f"] as LuaFunction).AsDelegate());
            }
        }

        [TestMethod]
        public void CallDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["f"] = new LuaCFunction(
                    delegate( IntPtr L )
                    {
                        LuaWrapper.lua_settop(L, 0);
                        LuaWrapper.lua_pushboolean(L, true);
                        return 1;
                    });

                var r = lua.Do("return f()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(true, r[0]);
            }
        }

        [TestMethod]
        public void KeepAliveObject()
        {
            using (var lua = new LuaBridge())
            {
                lua["o"] = new Tuple<bool, string>(true, "test");

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Tuple<bool, string> o = lua["o"] as Tuple<bool, string>;

                Assert.IsNotNull(o);
                Assert.AreEqual(true, o.Item1);
                Assert.AreEqual("test", o.Item2);
            }
        }

        private LuaCFunction generateDelegate( bool value )
        {
            return delegate( IntPtr L )
                {
                    LuaWrapper.lua_settop(L, 0);
                    LuaWrapper.lua_pushboolean(L, value);
                    return 1;
                };
        }

        [TestMethod]
        public void KeepAliveDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["f"] = generateDelegate(true);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var r = lua.Do("return f()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(true, r[0]);
            }
        }

        [TestMethod]
        public void NonBlockingDispose()
        {
            using (var lua = new InstrumentedLuaBridge(Instrumentations.Interruption))
            {
                var table = lua.NewTable();

                var thread = new Thread(() => { lua.Do("x = 0 CLR.Static['System.Threading.Thread'].Sleep(200) x = 1"); });

                thread.Start();

                Thread.Sleep(100);

                object xBefore = -1;

                lua.Interject(( L ) => { xBefore = lua["x"]; });

                table.Dispose();

                object xAfter = -1;

                lua.Interject(( L ) => { xAfter = lua["x"]; });

                thread.Join();

                Assert.AreEqual((double)0, xBefore);
                Assert.AreEqual((double)0, xAfter);
                Assert.AreEqual((double)1, lua["x"]);
            }
        }

        [TestMethod]
        public void PreventAccessingCollectedObject()
        {
            using (var lua = new LuaBridge())
            {
                lua.LoadLib("_G");

                lua.Do(@"function defer(f)
                             setmetatable({}, { __gc = f })
                         end");

                var table = lua.NewTable();

                lua["t"] = table;

                lua.Do("defer(function() t[1].ToString() end)");

                table[1] = new object();
            }
        }
    }
}
