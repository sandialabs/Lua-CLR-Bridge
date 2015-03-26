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
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DoStringTests : SandboxTestsBase
    {
        [TestMethod]
        public void ReturnEmpty()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("");

                Assert.AreEqual(0, r.Length);
            }
        }

        [TestMethod]
        public void ReturnNil()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return nil");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);
            }
        }

        [TestMethod]
        public void ReturnBoolean()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return true");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(true, (bool)r[0]);
            }
        }

        [TestMethod]
        public void ReturnNumber()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return 1");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual(1.0, (double)r[0]);
            }
        }

        [TestMethod]
        public void ReturnString()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return 'test'");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(string));
                Assert.AreEqual("test", r[0] as string);
            }
        }

        [TestMethod]
        public void ReturnTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return { 'test' }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual("test", (r[0] as LuaTable)[1]);
            }
        }

        [TestMethod]
        public void ReturnFunction()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return function(x) return x end");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaFunction));
            }
        }

        [TestMethod]
        public void ReturnMultiple()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return true, 'test'");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(true, (bool)r[0]);
                Assert.IsInstanceOfType(r[1], typeof(string));
                Assert.AreEqual("test", r[1] as string);
            }
        }

        [TestMethod]
        public void CompilerError()
        {
            using (var lua = CreateLuaBridge())
            {
                try
                {
                    var r = lua.Do("x");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaCompilerException));
                }
            }
        }

        [TestMethod]
        public void RuntimeError()
        {
            using (var lua = CreateLuaBridge())
            {
                try
                {
                    var r = lua.Do("return x.x");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaRuntimeException));
                }
            }
        }
    }
}
