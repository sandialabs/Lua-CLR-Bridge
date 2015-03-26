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
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OperatorOverloadTests : SandboxTestsBase
    {
        [TestMethod]
        public void TestOperators()
        {
            using (var lua = CreateLuaBridge())
            {
                Decimal x = new Decimal(2);
                Decimal y = new Decimal(3);
                Decimal z = new Decimal(2);

                lua["x"] = x;
                lua["y"] = y;
                lua["z"] = z;

                // arithmetic

                var r = lua.Do("return x + y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x + y, r[0]);

                r = lua.Do("return x - y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x - y, r[0]);

                r = lua.Do("return x * y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x * y, r[0]);

                r = lua.Do("return x / y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x / y, r[0]);

                r = lua.Do("return x % y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x % y, r[0]);

                r = lua.Do("return -x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(-x, r[0]);

                // equality

                r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == y, r[0]);

                r = lua.Do("return x ~= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != y, r[0]);

                r = lua.Do("return x == z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == z, r[0]);

                r = lua.Do("return x ~= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != z, r[0]);

                // comparison: x and y

                r = lua.Do("return x < y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x < y, r[0]);

                r = lua.Do("return x <= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x <= y, r[0]);

                r = lua.Do("return x > y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x > y, r[0]);

                r = lua.Do("return x >= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x >= y, r[0]);

                // comparison: y and x

                r = lua.Do("return y < x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y < x, r[0]);

                r = lua.Do("return y <= x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y <= x, r[0]);

                r = lua.Do("return y > x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y > x, r[0]);

                r = lua.Do("return y >= x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y >= x, r[0]);

                // comparison: x and z

                r = lua.Do("return x < z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x < z, r[0]);

                r = lua.Do("return x <= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x <= z, r[0]);

                r = lua.Do("return x > z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x > z, r[0]);

                r = lua.Do("return x >= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x >= z, r[0]);
            }
        }

        [Serializable]
        private struct ArithmeticA
        {
            public readonly int x;

            public ArithmeticA( int x )
            {
                this.x = x;
            }

            public static ArithmeticA operator +( ArithmeticA al, ArithmeticA ar )
            {
                return new ArithmeticA(al.x + ar.x);
            }

            public static ArithmeticA operator +( ArithmeticA a, ArithmeticB b )
            {
                return new ArithmeticA(a.x + b.x);
            }
        }

        [Serializable]
        private struct ArithmeticB
        {
            public readonly int x;

            public ArithmeticB( int x )
            {
                this.x = x;
            }

            public static ArithmeticB operator +( ArithmeticB b, ArithmeticA a )
            {
                return new ArithmeticB(b.x + a.x);
            }

            public static ArithmeticC operator +( ArithmeticB b, ArithmeticC c )
            {
                return new ArithmeticC(b.x + c.x);
            }

            public static ArithmeticD operator +( ArithmeticB b, ArithmeticD d )
            {
                return new ArithmeticD(b.x + d.x);
            }

            public static ArithmeticC operator +( ArithmeticC c, ArithmeticB b )
            {
                return new ArithmeticC(c.x + b.x);
            }

            public static ArithmeticD operator +( ArithmeticD d, ArithmeticB b )
            {
                return new ArithmeticD(d.x + b.x);
            }
        }

        [Serializable]
        private class ArithmeticC
        {
            public readonly int x;

            public ArithmeticC( int x )
            {
                this.x = x;
            }

            public static ArithmeticC operator +( ArithmeticC c, ArithmeticD d )
            {
                return new ArithmeticC(c.x + d.x);
            }
        }

        [Serializable]
        private class ArithmeticD
        {
            public readonly int x;

            public ArithmeticD( int x )
            {
                this.x = x;
            }

            public static ArithmeticD operator +( ArithmeticC c, ArithmeticD d )
            {
                return new ArithmeticD(c.x + d.x);
            }
        }

        [TestMethod]
        public void TestArithmetic()
        {
            using (var lua = CreateLuaBridge())
            {
                var a = new ArithmeticA(2);
                var b = new ArithmeticB(3);
                var c = new ArithmeticC(2);
                var d = new ArithmeticD(3);

                lua["a"] = a;
                lua["b"] = b;
                lua["c"] = c;
                lua["d"] = d;

                // successful, same operands

                var r = lua.Do("return a + a");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(a + a, r[0]);

                // missing, same operands

                try
                {
                    r = lua.Do("return b + b");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }

                // successful, different operands

                r = lua.Do("return a + b");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(a + b, r[0]);

                r = lua.Do("return b + a");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(b + a, r[0]);

                // ambiguous, one nil operand

                try
                {
                    r = lua.Do("return b + nil");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(AmbiguousMatchException));
                }

                try
                {
                    r = lua.Do("return nil + b");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(AmbiguousMatchException));
                }

                // ambiguous

                try
                {
                    r = lua.Do("return c + d");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(AmbiguousMatchException));
                }

                // missing, one nil operand

                try
                {
                    r = lua.Do("return d + nil");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }

                try
                {
                    r = lua.Do("return nil + c");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }
            }
        }

        [Serializable]
        private class ComparisonA
        {
            public static bool operator <( ComparisonA al, ComparisonA ar )
            {
                return true;
            }

            public static bool operator >( ComparisonA al, ComparisonA ar )
            {
                return false;
            }
        }

        [TestMethod]
        public void TestComparison()
        {
            using (var lua = CreateLuaBridge())
            {
                ComparisonA a = new ComparisonA();
                ComparisonA b = new ComparisonA();

                lua["a"] = a;
                lua["b"] = b;

                var r = lua.Do("return a < b");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(a < b, r[0]);
                Assert.AreNotEqual(b > a, r[0]);

                r = lua.Do("return a > b");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(b < a, r[0]);
                Assert.AreNotEqual(a > b, r[0]);
            }
        }

        [TestMethod]
        public void TestNoComparison()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["a"] = new object();

                try
                {
                    lua.Do("return a < a");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                    Assert.IsTrue(ex.Message.Contains("op_LessThan"));
                    Assert.IsTrue(ex.Message.Contains("op_GreaterThan"));
                }
            }
        }

        [TestMethod]
        public void TestReferenceEquality()
        {
            using (var lua = new LuaBridge())
            {
                var x = new object();
                var y = new object();
                var z = x;

                lua["x"] = x;
                lua["y"] = y;
                lua["z"] = z;

                var r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == y, r[0]);

                r = lua.Do("return x == z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == z, r[0]);
            }
        }

        private class OverloadedEqualitySameObject
        {
            public static bool operator ==( OverloadedEqualitySameObject lhs, OverloadedEqualitySameObject rhs )
            {
                return !Object.ReferenceEquals(lhs, rhs);
            }

            public static bool operator !=( OverloadedEqualitySameObject lhs, OverloadedEqualitySameObject rhs )
            {
                return !(lhs == rhs);
            }

            public override int GetHashCode() { return base.GetHashCode(); }

            public override bool Equals( object obj ) { return base.Equals(obj); }
        }

        [TestMethod]
        public void TestOverloadedEqualitySameObject()
        {
            using (var lua = new LuaBridge())
            {
                var x = new OverloadedEqualitySameObject();
                var y = x;
                lua["x"] = x;
                lua["y"] = y;

                var r = lua.Do("return x == x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
                Assert.AreNotEqual(x == x, r[0]);

                r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
                Assert.AreNotEqual(x == y, r[0]);
            }
        }

        [Serializable]
        private struct ValueEqualityUnimplemented
        {
            public readonly int x;

            public ValueEqualityUnimplemented( int x )
            {
                this.x = x;
            }
        }

        [TestMethod]
        public void TestValueEqualityUnimplemented()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ValueEqualityUnimplemented(1);
                var y = new ValueEqualityUnimplemented(1);

                lua["x"] = x;
                lua["y"] = y;

                try
                {
                    lua.Do("return x == y");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }

                try
                {
                    lua.Do("return x ~= y");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }
            }
        }

        [Serializable]
        private struct ValueEqualityImplemented
        {
            public readonly int x;

            public ValueEqualityImplemented( int x )
            {
                this.x = x;
            }

            public static bool operator ==( ValueEqualityImplemented lhs, ValueEqualityImplemented rhs )
            {
                return lhs.x == rhs.x;
            }

            public static bool operator !=( ValueEqualityImplemented lhs, ValueEqualityImplemented rhs )
            {
                return lhs.x != rhs.x;
            }

            public override int GetHashCode() { return base.GetHashCode(); }

            public override bool Equals( object obj ) { return base.Equals(obj); }
        }

        [TestMethod]
        public void TestValueEqualityImplemented()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ValueEqualityImplemented(1);
                var y = new ValueEqualityImplemented(2);
                var z = new ValueEqualityImplemented(1);

                lua["x"] = x;
                lua["y"] = y;
                lua["z"] = z;

                var r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == y, r[0]);

                r = lua.Do("return x ~= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != y, r[0]);

                r = lua.Do("return x == z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == z, r[0]);

                r = lua.Do("return x ~= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != z, r[0]);
            }
        }

        [TestMethod]
        public void TestValueEquality()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ValueEqualityUnimplemented(1);
                var y = new ValueEqualityImplemented(1);

                lua["x"] = x;
                lua["y"] = y;

                try
                {
                    lua.Do("return x == y");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }

                try
                {
                    lua.Do("return x ~= y");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }
            }
        }

        private enum Enum
        {
            X,
            Y,
        }

        [TestMethod]
        public void TestEnumComparison()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = Enum.X;
                var y = Enum.Y;
                var z = x;

                lua["x"] = x;
                lua["y"] = y;
                lua["z"] = z;

                // equality

                var r = lua.Do("return x == y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == y, r[0]);

                r = lua.Do("return x ~= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != y, r[0]);

                r = lua.Do("return x == z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x == z, r[0]);

                r = lua.Do("return x ~= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x != z, r[0]);

                // comparison: x and y

                r = lua.Do("return x < y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x < y, r[0]);

                r = lua.Do("return x <= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x <= y, r[0]);

                r = lua.Do("return x > y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x > y, r[0]);

                r = lua.Do("return x >= y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x >= y, r[0]);

                // comparison: y and x

                r = lua.Do("return y < x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y < x, r[0]);

                r = lua.Do("return y <= x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y <= x, r[0]);

                r = lua.Do("return y > x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y > x, r[0]);

                r = lua.Do("return y >= x");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(y >= x, r[0]);

                // comparison: x and z

                r = lua.Do("return x < z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x < z, r[0]);

                r = lua.Do("return x <= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x <= z, r[0]);

                r = lua.Do("return x > z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x > z, r[0]);

                r = lua.Do("return x >= z");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x >= z, r[0]);
            }
        }
    }
}
