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
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LuaBaseTests : SandboxTestsBase
    {
        [TestMethod]
        public void LuaBaseEquals()
        {
            using (var lua = CreateLuaBridge())
            {
                var t1 = lua.NewTable();

                using (var otherLua = CreateLuaBridge())
                {
                    var t2 = otherLua.NewTable();

                    Assert.AreNotEqual(t1, t2);
                }

                lua["t1"] = t1;

                lua.Do("t1_ = t1");

                Assert.AreEqual(t1, lua["t1_"]);

                var t3 = lua.NewTable();

                Assert.AreNotEqual(t1, t3);
            }
        }

        [TestMethod]
        public void LuaBaseGetHashCode()
        {
            using (var lua = CreateLuaBridge())
            {
                var t1 = lua.NewTable();

                using (var otherLua = CreateLuaBridge())
                {
                    var t2 = otherLua.NewTable();

                    Assert.AreNotEqual(t1.GetHashCode(), t2.GetHashCode());
                }

                lua["t1"] = t1;

                lua.Do("t1_ = t1");

                Assert.AreEqual(t1.GetHashCode(), lua["t1_"].GetHashCode());

                var t3 = lua.NewTable();

                Assert.AreNotEqual(t1.GetHashCode(), t3.GetHashCode());
            }
        }

        [TestMethod]
        public void LuaBaseToString()
        {
            using (var lua = CreateLuaBridge())
            {
                var t = lua.NewTable();
                var tString = t.ToString();

                Assert.IsNotNull(tString);
                Assert.IsTrue(tString.StartsWith("table: "));
            }
        }
    }
}
