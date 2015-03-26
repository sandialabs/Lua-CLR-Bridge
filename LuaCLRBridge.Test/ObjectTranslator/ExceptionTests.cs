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
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExceptionTests : SandboxTestsBase
    {
        [TestMethod]
        public void TestPcallCatchException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                var x = new int[] { 0, 1, 2 };

                lua["f"] = new Action(() =>
                {
                    throw new Exception("abc");
                });

                var r = lua.Do("return pcall(f.Invoke)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(Exception));
                Assert.AreEqual("abc", (r[1] as Exception).Message);
            }
        }
    }
}
