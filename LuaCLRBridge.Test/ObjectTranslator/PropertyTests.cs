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
    public class PropertyTests : SandboxTestsBase
    {
        [Serializable]
        private class PropertySet<T>
        {
            internal T _y;
            public T y { set { _y = value; } }
        }

        Type[] numericTypes =
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long),
                typeof(float), typeof(double),
            };

        [TestMethod]
        public void SetNumericProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                foreach (Type type in numericTypes)
                {
                    Type t = typeof(PropertySet<>).MakeGenericType(type);
                    dynamic x = Activator.CreateInstance(t);
                    x.y = 1;

                    lua["x"] = x;

                    var r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                    // object needs to be copied back from sandbox
                    x = lua["x"];
#endif

                    Assert.AreEqual((double)2, x._y);
                }
            }
        }

        [TestMethod]
        public void SetPropertyMismatchedType()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PropertySet<double>();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("x.y = nil");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }

                try
                {
                    var r = lua.Do("x.y = {}");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }
        }

        [Serializable]
        private class IndexedProperty
        {
            internal int _i;
            internal int _v;

            public int this[int i]
            {
                get { return -i; }
                set { _i = i; _v = value; }
            }
        }

        [TestMethod]
        public void GetIndexedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new IndexedProperty();

                lua["x"] = x;

                var r = lua.Do("return x.Item[1]");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)x[1], r[0]);
            }
        }

        [TestMethod]
        public void SetIndexedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new IndexedProperty();

                lua["x"] = x;

                var r = lua.Do("x.Item[1] = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as IndexedProperty;
#endif

                Assert.AreEqual((double)1, x._i);
                Assert.AreEqual((double)2, x._v);
            }
        }

        [Serializable]
        private class MultidimensonIndexedProperty
        {
            internal int _i;
            internal int _j;
            internal int _v;

            public int this[ int i, int j ]
            {
                get { return i - j; }
                set { _i = i; _j = j; _v = value; }
            }
        }

        [TestMethod]
        public void GetMultidimensionIndexedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MultidimensonIndexedProperty();

                lua["x"] = x;

                var r = lua.Do("return x.Item[{1, 2}]");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)x[1, 2], r[0]);
            }
        }

        [TestMethod]
        public void SetMultidimensionIndexedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MultidimensonIndexedProperty();

                lua["x"] = x;

                var r = lua.Do("x.Item[{1, 2}] = 3");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as MultidimensonIndexedProperty;
#endif

                Assert.AreEqual((double)1, x._i);
                Assert.AreEqual((double)2, x._j);
                Assert.AreEqual((double)3, x._v);
            }
        }

        [Serializable]
        private class DelegateProperty
        {
            public Func<int, int> y { get; set; }

            public int f( int i ) { return -i; }
        }

        [TestMethod]
        public void SetDelegateProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new DelegateProperty();

                lua["x"] = x;

                lua.Do("x.y = function( x ) return x + 1 end");

                var r = lua.Do("return x.y(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)2, r[0]);

#if false  // TODO
                lua.Do("x.y = x.f");

                r = lua.Do("return x.y(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)-1, r[0]);
#endif
            }
        }
    }
}
