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
    public class ConstructorTests : SandboxTestsBase
    {
        [Serializable]
        private class ImplicitNullaryConstructor
        {
        }

        [TestMethod]
        public void CallImplicitNullaryConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(ImplicitNullaryConstructor));

                lua["T"] = T;

                var r = lua.Do("return T()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(ImplicitNullaryConstructor));
            }
        }

        [Serializable]
        private class ExplicitNullaryConstructor
        {
            public ExplicitNullaryConstructor()
            {
                this.S = "test";
            }

            public string S { get; private set; }
        }

        [TestMethod]
        public void CallExplicitNullaryConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(ExplicitNullaryConstructor));

                lua["T"] = T;

                var r = lua.Do("return T()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(ExplicitNullaryConstructor));
                Assert.AreEqual("test", (r[0] as ExplicitNullaryConstructor).S);
            }
        }

        [Serializable]
        private class OverloadedConstructors
        {
            public OverloadedConstructors( string s )
            {
                this.S = s;
            }

            public OverloadedConstructors( double d )
            {
                this.S = d.ToString();
            }

            public string S { get; private set; }
        }

        [TestMethod]
        public void CallOverloadedConstructors()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(OverloadedConstructors));

                lua["T"] = T;

                var r = lua.Do("return T('test')");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(OverloadedConstructors));
                Assert.AreEqual("test", (r[0] as OverloadedConstructors).S);

                r = lua.Do("return T(1)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(OverloadedConstructors));
                Assert.AreEqual("1", (r[0] as OverloadedConstructors).S);

                try
                {
                    lua.Do("return T()");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMemberException));
                }
            }
        }
    }
}
