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
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AttributeTests : SandboxTestsBase
    {
        [Serializable]
        private class Base
        {
            public void f() { }
        }

        [Serializable]
        private class Derived : Base
        {
        }

        [LuaHideInheritedMembers]
        [Serializable]
        private class DerivedHidden : Base
        {
        }

        [TestMethod]
        public void CallHiddenInheritedMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new DerivedHidden();

                lua["x"] = x;

                object[] r;

                try
                {
                    r = lua.Do("return x.f()");

                    Assert.Fail();
                }
                catch (MissingMemberException)
                {
                    // expected
                }

                var y = new Derived();

                lua["y"] = y;

                r = lua.Do("return y.f()");

                Assert.AreEqual(0, r.Length);
            }
        }
    }
}
