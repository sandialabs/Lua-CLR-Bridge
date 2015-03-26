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
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MethodTests : SandboxTestsBase
    {
        [Serializable]
        private class Method
        {
            public string f( bool z ) { return MethodBase.GetCurrentMethod().MethodFormat("(ref {0})", z); }
        }

        [TestMethod]
        public void CallMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new Method();

                lua["x"] = x;

                var r = lua.Do("return x.f(true)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(true), r[0]);
            }
        }


        [Serializable]
        private class MethodRef
        {
            public string f( ref bool z ) { z = !z; return MethodBase.GetCurrentMethod().MethodFormat("(ref {0})", z); }
        }

        [TestMethod]
        public void CallMethodRef()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MethodRef();

                lua["x"] = x;

                var r = lua.Do("return x.f(true)");

                Assert.AreEqual(2, r.Length);
                bool b = true;
                Assert.AreEqual(x.f(ref b), r[0]);
                Assert.AreEqual(b, r[1]);
            }
        }

        [Serializable]
        private class MethodOut
        {
            public string f( out string x, out bool z ) { x = "test"; z = false; return MethodBase.GetCurrentMethod().MethodFormat("(out {0}, out {1})", x, z); }
        }

        [TestMethod]
        public void CallMethodOut()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MethodOut();

                lua["x"] = x;

                var r = lua.Do("return x.f('', true)");

                Assert.AreEqual(3, r.Length);
                string s;
                bool b;
                Assert.AreEqual(x.f(out s, out b), r[0]);
                Assert.AreEqual(s, r[1]);
                Assert.AreEqual(b, r[2]);
            }
        }

        [Serializable]
        private class MethodDefault
        {
            public string f( bool z, string y = "test" ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", z, y); }
        }

        [TestMethod]
        public void CallMethodDefault()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MethodDefault();

                lua["x"] = x;

                var r = lua.Do("return x.f(true)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(true), r[0]);
            }
        }
    }
}
