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
    public class FieldTests : SandboxTestsBase
    {
        [Serializable]
        public class Field<T>
        {
            public T y;
        }

        Type[] numericTypes =
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long),
                typeof(float), typeof(double),
            };

        [TestMethod]
        public void SetNumericField()
        {
            using (var lua = CreateLuaBridge())
            {
                foreach (Type type in numericTypes)
                {
                    Type t = typeof(Field<>).MakeGenericType(type);
                    dynamic x = Activator.CreateInstance(t);
                    x.y = 1;

                    lua["x"] = x;

                    var r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                    // object needs to be copied back from sandbox
                    x = lua["x"];
#endif

                    Assert.AreEqual((double)2, x.y);
                }
            }
        }

        [TestMethod]
        public void SetFieldMismatchedType()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new Field<double>();

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
        private class DelegateField
        {
            public Func<int, int> y = null;

            public int f( int i ) { return -i; }
        }

        [TestMethod]
        public void SetDelegateField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new DelegateField();

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
