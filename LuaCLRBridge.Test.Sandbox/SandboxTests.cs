/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Test.Sandbox
{
    using System;
    using System.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SandboxTests
    {
        [SecuritySafeCritical]
        [TestMethod]
        public void TestCLRInt64Sandbox()
        {
            using (var sandbox = new Sandbox())
            using (var lua = sandbox.CreateLuaBridge())
            {
                var r = lua.Do("x = CLR.Cast.Int64{'Double'}(1); return x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Int64));
                Assert.AreEqual(1L, r[0]);

                r = lua.Do("return x + x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Int64));
                Assert.AreEqual(2L, r[0]);
            }
        }

        [Serializable]
        public class SecurityAttributeMethods
        {
            [SecuritySafeCritical]
            public void f() { }

            [SecurityCritical]
            public void g() { }
        }

        [SecuritySafeCritical]
        [TestMethod]
        public void TestSecurityAttributeMethodsSandbox()
        {
            using (var sandbox = new Sandbox())
            using (var lua = sandbox.CreateLuaBridge())
            {
                lua["x"] = new SecurityAttributeMethods();

                var r = lua.Do("return x.f()");

                Assert.AreEqual(0, r.Length);

                try
                {
                    r = lua.Do("return x.g()");

                    Assert.Fail();
                }
                catch (SecurityException)
                {
                    // expected
                }
            }
        }
    }
}
