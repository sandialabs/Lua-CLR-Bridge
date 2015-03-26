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
    public class NullableTests : SandboxTestsBase
    {
        [TestMethod]
        public void GetNullableArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int?[] { 0, 1, null, 3 };

                lua["x"] = x;

                var r = lua.Do("return x[1]");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual((double)1, r[0]);

                r = lua.Do("return x[2]");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);
            }
        }

        [TestMethod]
        public void SetNullableArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int?[] { 9, 8, 7 };

                lua["x"] = x;

                var r = lua.Do("x[1] = 1");

#if !NO_SANDBOX
                // array needs to be copied back from sandbox
                x = lua["x"] as int?[];
#endif

                Assert.AreEqual(1, x[1]);

                r = lua.Do("x[1] = nil");

#if !NO_SANDBOX
                // array needs to be copied back from sandbox
                x = lua["x"] as int?[];
#endif

                Assert.IsNull(x[1]);
            }
        }

        [Serializable]
        private class NullableField
        {
            public int? y;
        }

        [TestMethod]
        public void GetSetNullableField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NullableField() { y = 0 };

                lua["x"] = x;

                var r = lua.Do("return x.y == 0");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);

                r = lua.Do("x.y = 1");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as NullableField;
#endif

                Assert.AreEqual(0, r.Length);
                Assert.AreEqual(1, x.y);

                r = lua.Do("x.y = null");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as NullableField;
#endif

                Assert.AreEqual(0, r.Length);
                Assert.AreEqual(null, x.y);

                r = lua.Do("return x.y == null");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }

        [Serializable]
        private class NullableProperty
        {
            public int? y { get; set; }
        }

        [TestMethod]
        public void GetSetNullableProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NullableProperty() { y = 0 };

                lua["x"] = x;

                var r = lua.Do("return x.y == 0");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);

                r = lua.Do("x.y = 1");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as NullableProperty;
#endif

                Assert.AreEqual(0, r.Length);
                Assert.AreEqual(1, x.y);

                r = lua.Do("x.y = null");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as NullableProperty;
#endif

                Assert.AreEqual(0, r.Length);
                Assert.AreEqual(null, x.y);

                r = lua.Do("return x.y == null");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }

        [Serializable]
        private class NullablePrimitiveParamMethod
        {
            public string f( int? x ) { return "int? " + x; }
        }

        [TestMethod]
        public void CallNullablePrimitiveParamMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NullablePrimitiveParamMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f(null)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(null), r[0]);

                r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);
            }
        }

        [Serializable]
        private class NullableNonprimitiveParamMethod
        {
            public string f( DateTimeOffset? x ) { return "DateTimeOffset? " + x; }
        }

        [TestMethod]
        public void CallNullableNonprimitiveParamMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NullableNonprimitiveParamMethod();
                var z = new DateTimeOffset();

                lua["x"] = x;
                lua["z"] = z;

                var r = lua.Do("return x.f(null)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(null), r[0]);

                r = lua.Do("return x.f(z)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(z), r[0]);
            }
        }

        [Serializable]
        private class NullableParamOverloadedMethod
        {
            public string f( int? x ) { return "int? " + x; }
            public string f( int x ) { return "int " + x; }
        }

        [TestMethod]
        public void CallNullableParamOverloadedMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NullableParamOverloadedMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f(null)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(null), r[0]);

                r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);
            }
        }
    }
}
