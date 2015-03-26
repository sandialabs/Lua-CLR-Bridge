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
    using System;
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CLRInt64Tests : SandboxTestsBase
    {
        [TestMethod]
        public void TestCheckedCast()
        {
            Int64 x = Int64.MaxValue;
            Double y = (double)x + 1;

            try
            {
                x = checked((Int64)y);

                Assert.Fail();
            }
            catch (OverflowException)
            {
                // expected
            }
        }

        private Int64 value( CLRInt64 wrapper )
        {
            return (Int64)new PrivateObject(wrapper).GetField("_value");
        }

        [TestMethod]
        public void TestAdd()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(3);
            object c = a + b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) + value(b), value((CLRInt64)c));

            double d = 4.0;

            c = a + d;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) + d, value((CLRInt64)c));

            c = d + a;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(d + value(a), value((CLRInt64)c));

            foreach (var t in new Tuple<Int64, double>[] {
                Tuple.Create(1L, 0.5),
                Tuple.Create(Int64.MaxValue, 2.0),
                Tuple.Create(Int64.MinValue, -2.0) })
            {
                a = new CLRInt64(t.Item1);
                d = t.Item2;

                c = a + d;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(value(a) + d, c);

                c = d + a;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(d + value(a), c);
            }
        }

        [TestMethod]
        public void TestSubtract()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(3);
            object c = a - b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) - value(b), value((CLRInt64)c));

            double d = 4.0;

            c = a - d;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) - d, value((CLRInt64)c));

            c = d - a;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(d - value(a), value((CLRInt64)c));

            foreach (var t in new Tuple<Int64, double>[] {
                Tuple.Create(1L, 0.5),
                Tuple.Create(Int64.MaxValue, -2.0),
                Tuple.Create(Int64.MinValue, 2.0) })
            {
                a = new CLRInt64(t.Item1);
                d = t.Item2;

                c = a - d;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(value(a) - d, c);

                c = d - a;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(d - value(a), c);
            }
        }

        [TestMethod]
        public void TestMultiply()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(3);
            object c = a * b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) * value(b), value((CLRInt64)c));

            double d = 4.0;

            c = a * d;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) * d, value((CLRInt64)c));

            c = d * a;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(d * value(a), value((CLRInt64)c));

            foreach (var t in new Tuple<Int64, double>[] {
                Tuple.Create(1L, 0.5),
                Tuple.Create(Int64.MaxValue, -2.0),
                Tuple.Create(Int64.MinValue, 2.0) })
            {
                a = new CLRInt64(t.Item1);
                d = t.Item2;

                c = a * d;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(value(a) * d, c);

                c = d * a;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(d * value(a), c);
            }
        }

        [TestMethod]
        public void TestDivide()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(3);
            object c = a / b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) / value(b), value((CLRInt64)c));

            double d = 4.0;

            c = a / d;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) / (Int64)d, value((CLRInt64)c));

            c = d / a;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(d / value(a), value((CLRInt64)c));

            d = 0.25;

            c = a / d;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual(value(a) / d, c);

            c = d / a;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual(d / value(a), c);
        }

        [TestMethod]
        public void TestModulus()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(3);
            object c = a % b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) % value(b), value((CLRInt64)c));

            double d = 4.0;

            c = a % d;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a) % (Int64)d, value((CLRInt64)c));

            c = d % a;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(d % value(a), value((CLRInt64)c));

            d = 0.25;

            c = a % d;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual(value(a) % d, c);

            c = d % a;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual(d % value(a), c);
        }

        [TestMethod]
        public void TestNegation()
        {
            CLRInt64 a = new CLRInt64(2);
            
            object b = -a;

            Assert.IsInstanceOfType(b, typeof(CLRInt64));
            Assert.AreEqual(-value(a), value((CLRInt64)b));

            object c = -(CLRInt64)b;

            Assert.IsInstanceOfType(c, typeof(CLRInt64));
            Assert.AreEqual(value(a), value((CLRInt64)c));

            CLRInt64 d = new CLRInt64(Int64.MinValue);

            object e = -d;

            Assert.IsInstanceOfType(e, typeof(double));
            Assert.AreEqual(-(double)Int64.MinValue, (double)e);
        }

        [TestMethod]
        public void TestEquality()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(2);
            CLRInt64 c = new CLRInt64(3);

            Assert.AreEqual(value(a) == value(b), a == b);
            Assert.AreEqual(value(a) == value(c), a == c);

            double d = 2.0;

            Assert.AreEqual(value(a) == d, a == d);
            Assert.AreEqual(value(c) == d, c == d);
        }

        [TestMethod]
        public void TestInequality()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(2);
            CLRInt64 c = new CLRInt64(3);

            Assert.AreEqual(value(a) <= value(b), a <= b);
            Assert.AreEqual(value(a) <= value(c), a <= c);

            double d = 2.0;

            Assert.AreEqual(value(a) <= d, a <= d);
            Assert.AreEqual(value(c) <= d, c <= d);
        }

        [TestMethod]
        public void TestStrictInequality()
        {
            CLRInt64 a = new CLRInt64(2);
            CLRInt64 b = new CLRInt64(2);
            CLRInt64 c = new CLRInt64(3);

            Assert.AreEqual(value(a) < value(b), a < b);
            Assert.AreEqual(value(a) < value(c), a < c);

            double d = 2.0;

            Assert.AreEqual(value(a) < d, a < d);
            Assert.AreEqual(value(c) < d, c < d);
        }

        private struct BinaryOperatorTestEntry
        {
            public readonly object Lhs;
            public readonly object Rhs;
            public readonly object Add;
            public readonly object Sub;
            public readonly object Mult;
            public readonly object Div;
            public readonly object Mod;
            public readonly object Eq;
            public readonly object Neq;
            public readonly object Lt;
            public readonly object Le;
            public readonly object Gt;
            public readonly object Ge;

            public BinaryOperatorTestEntry( object lhs, object rhs, object add = null, object sub = null, object mult = null, object div = null, object mod = null, object eq = null, object neq = null, object lt = null, object le = null, object gt = null, object ge = null )
            {
                this.Lhs = lhs;
                this.Rhs = rhs;
                this.Add = add;
                this.Sub = sub;
                this.Mult = mult;
                this.Div = div;
                this.Mod = mod;
                this.Eq = eq;
                this.Neq = neq;
                this.Lt = lt;
                this.Le = le;
                this.Gt = gt;
                this.Ge = ge;
            }
        }

        [TestMethod]
        public void TestInLua()
        {
            using (var lua = CreateLuaBridge())
            {
                var min = Int64.MinValue;
                var max = Int64.MaxValue;

                foreach (var entry in new BinaryOperatorTestEntry[]
                {
                    // decreasing values
                    new BinaryOperatorTestEntry(3L,  2L,  add: 3L  + 2L, sub: 3L - 2L, mult: 3L * 2L, div: 3L / 2L, mod: 3L % 2L, eq: 3L == 2L, neq: 3L != 2L, lt: 3L < 2L, le: 3L <= 2L, gt: 3L > 2L, ge: 3L >= 2L),
                    new BinaryOperatorTestEntry(3L,  2.0, add: 3L  + 2,  sub: 3L - 2,  mult: 3L * 2,  div: 3L / 2,  mod: 3L % 2,  eq: false,    neq: true,     lt: 3L < 2,  le: 3L <= 2,  gt: 3L > 2,  ge: 3L >= 2),
                    new BinaryOperatorTestEntry(3.0, 2L,  add: 3   + 2L, sub: 3  - 2L, mult: 3  * 2L, div: 3  / 2L, mod: 3  % 2L, eq: false,    neq: true,     lt: 3  < 2L, le: 3  <= 2L, gt: 3  > 2L, ge: 3  >= 2L),

                    // increasing values
                    new BinaryOperatorTestEntry(1L,  2L,  add: 1L  + 2L, sub: 1L - 2L, mult: 1L * 2L, div: 1L / 2L, mod: 1L % 2L, eq: 1L == 2L, neq: 1L != 2L, lt: 1L < 2L, le: 1L <= 2L, gt: 1L > 2L, ge: 1L >= 2L),
                    new BinaryOperatorTestEntry(1L,  2.0, add: 1L  + 2,  sub: 1L - 2,  mult: 1L * 2,  div: 1L / 2,  mod: 1L % 2,  eq: false,    neq: true,     lt: 1L < 2,  le: 1L <= 2,  gt: 1L > 2,  ge: 1L >= 2),
                    new BinaryOperatorTestEntry(1.0, 2L,  add: 1   + 2L, sub: 1  - 2L, mult: 1  * 2L, div: 1  / 2L, mod: 1  % 2L, eq: false,    neq: true,     lt: 1  < 2L, le: 1  <= 2L, gt: 1  > 2L, ge: 1  >= 2L),

                    // equal values
                    new BinaryOperatorTestEntry(1L,  1L,  eq: 1L == 1L, neq: 1L != 1L, lt: 1L < 1L, le: 1L <= 1L, gt: 1L > 1L, ge: 1L >= 1L),
                    new BinaryOperatorTestEntry(1L,  1.0, eq: false,    neq: true,     lt: 1L < 1,  le: 1L <= 1,  gt: 1L > 1,  ge: 1L >= 1),
                    new BinaryOperatorTestEntry(1.0, 1L,  eq: false,    neq: true,     lt: 1  < 1L, le: 1  <= 1L, gt: 1  > 1L, ge: 1  >= 1L),

                    // overflows
                    new BinaryOperatorTestEntry(max, 1L,  add: (double)max + 1L),
                    new BinaryOperatorTestEntry(max, 1.0, add: (double)max + 1),
                    new BinaryOperatorTestEntry(min, 1L,  sub: (double)min - 1L),
                    new BinaryOperatorTestEntry(min, 1.0, sub: (double)min - 1),
                    new BinaryOperatorTestEntry(max, 2L,  mult: (double)max * 2L),
                    new BinaryOperatorTestEntry(max, 2.0, mult: (double)max * 2),
                    new BinaryOperatorTestEntry(max, 0.5, div: (double)max / 0.5),
                })
                {
                    lua["lhs"] = entry.Lhs;
                    lua["rhs"] = entry.Rhs;

                    foreach (var pair in new Tuple<object, string>[]
                        {
                            Tuple.Create(entry.Add, "+"),
                            Tuple.Create(entry.Sub, "-"),
                            Tuple.Create(entry.Mult, "*"),
                            Tuple.Create(entry.Div, "/"),
                            Tuple.Create(entry.Mod, "%"),
                            Tuple.Create(entry.Eq, "=="),
                            Tuple.Create(entry.Neq, "~="),
                            Tuple.Create(entry.Lt, "<"),
                            Tuple.Create(entry.Le, "<="),
                            Tuple.Create(entry.Gt, ">"),
                            Tuple.Create(entry.Ge, ">="),
                        })
                    {
                        var expected = pair.Item1;
                        var op = pair.Item2;

                        if (expected != null)
                        {
                            var r = lua.Do("return lhs " + op + " rhs");

                            var message = String.Format("{0} ({1}) {4} {2} ({3})", entry.Lhs, entry.Lhs.GetType(), entry.Rhs, entry.Rhs.GetType(), op);

                            Assert.AreEqual(1, r.Length, message);
                            Assert.IsInstanceOfType(r[0], expected.GetType(), message);
                            Assert.AreEqual(expected, r[0], message);
                        }
                    }
                }

                foreach (var entry in new Tuple<object, object>[]
                {
                    new Tuple<object, object>(0L,  -0L),
                    new Tuple<object, object>(min, -(double)min),
                    new Tuple<object, object>(max, -max),
                })
                {
                    var operand = entry.Item1;
                    var expected = entry.Item2;

                    lua["operand"] = operand;

                    var r = lua.Do("return -operand");

                    var message = String.Format("-{0} ({1})", operand, operand.GetType());

                    Assert.AreEqual(1, r.Length, message);
                    Assert.IsInstanceOfType(r[0], expected.GetType(), message);
                    Assert.AreEqual(expected, r[0], message);
                }
            }
        }
    }
}
