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
    public class LuaFunctionTests : SandboxTestsBase
    {
        [TestMethod]
        public void FunctionCall()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return function(x, y) return x .. y end");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaFunction));

                r = (r[0] as LuaFunction).Call("x", "y");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(string));
                Assert.AreEqual("xy", r[0] as string);
            }
        }

        [TestMethod]
        public void FunctionCallReturnMultiple()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return function(x, y) return y, x end");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaFunction));

                r = (r[0] as LuaFunction).Call("test", true);

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(true, (bool)r[0]);
                Assert.IsInstanceOfType(r[1], typeof(string));
                Assert.AreEqual("test", r[1] as string);
            }
        }

        [Serializable]
        private class CFunctionThrows
        {
            public static int Call( IntPtr L )
            {
                throw new NotSupportedException();
            }
        }

        [TestMethod]
        public void CallCFunctionThrows()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["f"] = lua.NewFunction(CFunctionThrows.Call);

                try
                {
                    lua.Do("return f(1)");
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(NotSupportedException));
                }
            }
        }

        [Serializable]
        private class SafeCFunctionThrows
        {
            public static object[] Call( LuaBridgeBase bridge, params object[] args )
            {
                throw new NotSupportedException();
            }
        }

        [TestMethod]
        public void CallSafeCFunctionThrows()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["f"] = lua.NewFunction(SafeCFunctionThrows.Call);

                try
                {
                    lua.Do("return f(1)");
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(NotSupportedException));
                }
            }
        }

        [TestMethod]
        public void FunctionAlreadyDisposed()
        {
            LuaFunction f;

            using (var lua = CreateLuaBridge())
            {
                f = lua.Do("return function( ) return true end")[0] as LuaFunction;
            }

            try
            {
                var r = f.Call();
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ObjectDisposedException));
            }
        }
    }
}
