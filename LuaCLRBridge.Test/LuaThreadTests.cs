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
    public class LuaThreadTests : SandboxTestsBase
    {
        [TestMethod]
        public void ThreadAlreadyDisposed1()
        {
            LuaThread t;

            LuaThreadBridge luaThread;

            using (var lua = CreateLuaBridge())
            {
                t = lua.NewThread();

                luaThread = t.CreateBridge();
            }

            try
            {
                using (luaThread)
                {
                    luaThread.Do("return true");
                }
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ObjectDisposedException));
            }

            try
            {
                using (luaThread = t.CreateBridge())
                {
                    luaThread.Do("return true");
                }
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ObjectDisposedException));
            }
        }

        [TestMethod]
        public void ThreadAlreadyDisposed2()
        {
            using (var lua = CreateLuaBridge())
            {
                LuaThreadBridge luaThread;

                using (LuaThread t = lua.NewThread())
                {
                    luaThread = t.CreateBridge();
                }

                luaThread.Do("return true");
            }
        }
    }
}
