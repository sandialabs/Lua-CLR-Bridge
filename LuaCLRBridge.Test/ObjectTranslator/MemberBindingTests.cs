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
    public class MemberBindingTests : SandboxTestsBase
    {
        private class TypeMemberBindingHints
        {
            public LuaTable table { get; private set; }

            public TypeMemberBindingHints( LuaTable table )
            {
                this.table = table;
            }
        }

        [TestMethod]
        public void TypeMemberBindingHintsTest()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["T"] = new CLRStaticContext(typeof(TypeMemberBindingHints));

                var r = lua.Do("return T{}{}({ true }).table");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(1, (r[0] as LuaTable).Count);

                r = lua.Do("return T{}{}({ true }){}.table");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(1, (r[0] as LuaTable).Count);

                try
                {
                    lua.Do("return T{}{}({ true }){}{}.table");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ObjectTranslatorException));
                }
            }
        }

        [TestMethod]
        public void InstanceMemberBindingHintsTest()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["x"] = new object();

                var r = lua.Do("return x{}.ToString()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("System.Object", r[0]);

                try
                {
                    lua.Do("return x{}{}.ToString()");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ObjectTranslatorException));
                }
            }
        }

        [Serializable]
        private struct SpecialName
        {
            private int x;

            public SpecialName( int x )
            {
                this.x = x;
            }

            public static SpecialName operator +( SpecialName sl, SpecialName sr )
            {
                return new SpecialName(sl.x + sr.x);
            }
        }

        [TestMethod]
        public void SpecialNameTest()
        {
            using (var lua = CreateLuaBridge())
            {
                SpecialName x = new SpecialName(2);
                SpecialName y = new SpecialName(3);
                
                lua["c"] = new CLRStaticContext(typeof(SpecialName));
                lua["x"] = x;
                lua["y"] = y;

                var r = lua.Do("return c{SpecialName = true}.op_Addition(x, y)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x + y, r[0]);

                try
                {
                    lua.Do("return c.op_Addition(x, y)");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMemberException));
                }

            }
        }

        [Serializable]
        private struct NonSpecialName
        {
            private int x;

            public NonSpecialName( int x )
            {
                this.x = x;
            }

            public static NonSpecialName op_Addition( NonSpecialName sl, NonSpecialName sr )
            {
                return new NonSpecialName(sl.x + sr.x);
            }
        }

        [TestMethod]
        public void NonSpecialNameTest()
        {
            using (var lua = CreateLuaBridge())
            {
                NonSpecialName x = new NonSpecialName(2);
                NonSpecialName y = new NonSpecialName(3);

                lua["c"] = new CLRStaticContext(typeof(NonSpecialName));
                lua["x"] = x;
                lua["y"] = y;

                var r = lua.Do("return c.op_Addition(x, y)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(NonSpecialName.op_Addition(x, y), r[0]);

                try
                {
                    lua.Do("return c{SpecialName = true}.op_Addition(x, y)"); 
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMemberException));
                }
            }
        }
    }
}
