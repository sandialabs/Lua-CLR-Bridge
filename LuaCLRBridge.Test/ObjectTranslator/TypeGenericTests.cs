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
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TypeGenericTests : SandboxTestsBase
    {
        [TestMethod]
        public void ConstructGenericType()
        {
            using (var lua = CreateLuaBridge())
            {
                var l = new CLRStaticContext(typeof(List<>));
                var i = typeof(Int32);

                lua["List"] = l;
                lua["Int32"] = i;

                var r = lua.Do("return List{Int32}()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(List<Int32>));

                try
                {
                    r = lua.Do("return List{Int32, Int32}()");
                    Assert.Fail();
                }
                catch (BindingHintsException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void ConstructGenericTypeFromCLRBridge()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("List = CLR.Static['System.Collections.Generic.List`1']");

                Assert.AreEqual(0, r.Length);
                Assert.IsNotNull(lua["List"]);

                r = lua.Do("Int32 = CLR.Type['System.Int32']");

                Assert.AreEqual(0, r.Length);
                Assert.IsNotNull(lua["Int32"]);

                r = lua.Do("return List{Int32}()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(List<Int32>));
            }
        }

        // TODO: test passing nonsense hints
    }
}
