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
    public class CLRUInt64Tests : SandboxTestsBase
    {
        [TestMethod]
        public void TestCheckedCast()
        {
            UInt64 x = UInt64.MaxValue;
            Double y = (double)x + 1;

            try
            {
                x = checked((UInt64)y);

                Assert.Fail();
            }
            catch (OverflowException)
            {
                // expected
            }
        }

        private UInt64 value( CLRUInt64 wrapper )
        {
            return (UInt64)new PrivateObject(wrapper).GetField("_value");
        }

        [TestMethod]
        public void TestAdd()
        {
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(3);
            object c = a + b;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) + value(b), value((CLRUInt64)c));

            double d = 4.0;

            c = a + d;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) + d, value((CLRUInt64)c));

            c = d + a;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(d + value(a), value((CLRUInt64)c));

            foreach (var t in new Tuple<UInt64, double>[] {
                Tuple.Create(1UL, 0.5),
                Tuple.Create(UInt64.MaxValue, 2.0),
                Tuple.Create(UInt64.MinValue, -2.0) })
            {
                a = new CLRUInt64(t.Item1);
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
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(3);
            object c = a - b;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual((double)value(a) - (double)value(b), (double)c);

            double d = 4.0;

            c = a - d;

            Assert.IsInstanceOfType(c, typeof(double));
            Assert.AreEqual((double)value(a) - (double)d, (double)c);

            c = d - a;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(d - value(a), value((CLRUInt64)c));

            foreach (var t in new Tuple<UInt64, double>[] {
                Tuple.Create(1UL, 0.5),
                Tuple.Create(UInt64.MaxValue, -2.0),
                Tuple.Create(UInt64.MinValue, 2.0) })
            {
                a = new CLRUInt64(t.Item1);
                d = t.Item2;

                c = a - d;

                Assert.IsInstanceOfType(c, typeof(double));
                Assert.AreEqual(value(a) - d, c);

                if (a != 0)
                {
                    c = d - a;

                    Assert.IsInstanceOfType(c, typeof(double));
                    Assert.AreEqual(d - value(a), c);
                }
            }
        }

        [TestMethod]
        public void TestMultiply()
        {
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(3);
            object c = a * b;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) * value(b), value((CLRUInt64)c));

            double d = 4.0;

            c = a * d;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) * d, value((CLRUInt64)c));

            c = d * a;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(d * value(a), value((CLRUInt64)c));

            foreach (var t in new Tuple<UInt64, double>[] {
                Tuple.Create(1UL, 0.5),
                Tuple.Create(UInt64.MaxValue, -2.0),
                Tuple.Create(UInt64.MaxValue, 2.0) })
            {
                a = new CLRUInt64(t.Item1);
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
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(3);
            object c = a / b;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) / value(b), value((CLRUInt64)c));

            double d = 4.0;

            c = a / d;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) / (UInt64)d, value((CLRUInt64)c));

            c = d / a;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(d / value(a), value((CLRUInt64)c));

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
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(3);
            object c = a % b;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) % value(b), value((CLRUInt64)c));

            double d = 4.0;

            c = a % d;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(value(a) % (UInt64)d, value((CLRUInt64)c));

            c = d % a;

            Assert.IsInstanceOfType(c, typeof(CLRUInt64));
            Assert.AreEqual(d % value(a), value((CLRUInt64)c));

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
            CLRUInt64 a = new CLRUInt64(2);
            
            object b = -a;

            Assert.IsInstanceOfType(b, typeof(double));
            Assert.AreEqual(-(double)value(a), (double)b);
        }

        [TestMethod]
        public void TestEquality()
        {
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(2);
            CLRUInt64 c = new CLRUInt64(3);

            Assert.AreEqual(value(a) == value(b), a == b);
            Assert.AreEqual(value(a) == value(c), a == c);

            double d = 2.0;

            Assert.AreEqual(value(a) == d, a == d);
            Assert.AreEqual(value(c) == d, c == d);
        }

        [TestMethod]
        public void TestInequality()
        {
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(2);
            CLRUInt64 c = new CLRUInt64(3);

            Assert.AreEqual(value(a) <= value(b), a <= b);
            Assert.AreEqual(value(a) <= value(c), a <= c);

            double d = 2.0;

            Assert.AreEqual(value(a) <= d, a <= d);
            Assert.AreEqual(value(c) <= d, c <= d);
        }

        [TestMethod]
        public void TestStrictInequality()
        {
            CLRUInt64 a = new CLRUInt64(2);
            CLRUInt64 b = new CLRUInt64(2);
            CLRUInt64 c = new CLRUInt64(3);

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
                var min = UInt64.MinValue;
                var max = UInt64.MaxValue;
                var smin = Int64.MinValue;
                var smax = Int64.MaxValue;

                foreach (var entry in new BinaryOperatorTestEntry[]
                {
                    // decreasing values
                    new BinaryOperatorTestEntry(3UL, 2UL, add: 3UL + 2UL, sub: 3UL - 2UL, mult: 3UL * 2UL, div: 3UL / 2UL, mod: 3UL % 2UL, eq: 3UL == 2UL, neq: 3UL != 2UL, lt: 3UL < 2UL, le: 3UL <= 2UL, gt: 3UL > 2UL, ge: 3UL >= 2UL),
                    new BinaryOperatorTestEntry(3UL, 2.0, add: 3UL + 2,   sub: 3UL - 2,   mult: 3UL * 2,   div: 3UL / 2,   mod: 3UL % 2,   eq: false,      neq: true,       lt: 3UL < 2,   le: 3UL <= 2,   gt: 3UL > 2,   ge: 3UL >= 2),
                    new BinaryOperatorTestEntry(3.0, 2UL, add: 3   + 2UL, sub: 3   - 2UL, mult: 3   * 2UL, div: 3   / 2UL, mod: 3   % 2UL, eq: false,      neq: true,       lt: 3   < 2UL, le: 3   <= 2UL, gt: 3   > 2UL, ge: 3   >= 2UL),
                    new BinaryOperatorTestEntry(3UL,  2L, add: (double)3UL + 2L,  sub: (double)3UL - 2L,  mult: (double)3UL * 2L,  div: (double)3UL / 2L,  mod: (double)3UL % 2L,  eq: 3UL == 2L,  neq: 3UL != 2L,  lt: 3UL < 2L,  le: 3UL <= 2L,  gt: 3UL > 2L,  ge: 3UL >= 2L),
                    new BinaryOperatorTestEntry(3L,  2UL, add: (double)3L  + 2UL, sub: (double)3L  - 2UL, mult: (double)3L  * 2UL, div: (double)3L  / 2UL, mod: (double)3L  % 2UL, eq: 3L == 2UL,  neq: 3L  != 2UL, lt: 3L  < 2UL, le: 3L  <= 2UL, gt: 3L  > 2UL, ge: 3L  >= 2UL),

                    // increasing values
                    new BinaryOperatorTestEntry(1UL, 2UL, add: 1UL  + 2UL, sub: (double)1UL - 2UL, mult: 1UL * 2UL, div: 1UL / 2UL, mod: 1UL % 2UL, eq: 1UL == 2UL, neq: 1UL != 2UL, lt: 1UL < 2UL, le: 1UL <= 2UL, gt: 1UL > 2UL, ge: 1UL >= 2UL),
                    new BinaryOperatorTestEntry(1UL, 2.0, add: 1UL  + 2,   sub: (double)1UL - 2,   mult: 1UL * 2,   div: 1UL / 2,   mod: 1UL % 2,   eq: false,      neq: true,       lt: 1UL < 2,   le: 1UL <= 2,   gt: 1UL > 2,   ge: 1UL >= 2),
                    new BinaryOperatorTestEntry(1.0, 2UL, add: 1   + 2UL,  sub: (double)1   - 2UL, mult: 1   * 2UL, div: 1   / 2UL, mod: 1   % 2UL, eq: false,      neq: true,       lt: 1  < 2UL,  le: 1  <= 2UL,  gt: 1  > 2UL,  ge: 1  >= 2UL),
                    new BinaryOperatorTestEntry(1UL,  2L, add: (double)1UL + 2L,   sub: (double)1UL - 2L,  mult: (double)1UL * 2L,  div: (double)1UL / 2L,  mod: (double)1UL % 2L,  eq: 1UL == 2L,  neq: 1UL != 2L,  lt: 1UL < 2L,  le: 1UL <= 2L,  gt: 1UL > 2L,  ge: 1UL >= 2L),
                    new BinaryOperatorTestEntry(1L,  2UL, add: (double)1L  + 2UL,  sub: (double)1L  - 2UL, mult: (double)1L  * 2UL, div: (double)1L  / 2UL, mod: (double)1L  % 2UL, eq: 1L == 2UL,  neq: 1L  != 2UL, lt: 1L  < 2UL, le: 1L  <= 2UL, gt: 1L  > 2UL, ge: 1L  >= 2UL),

                    // equal values
                    new BinaryOperatorTestEntry(1UL, 1UL, eq: 1UL == 1UL, neq: 1UL != 1UL, lt: 1UL < 1UL, le: 1UL <= 1UL, gt: 1UL > 1UL, ge: 1UL >= 1UL),
                    new BinaryOperatorTestEntry(1UL, 1.0, eq: false,      neq: true,       lt: 1UL < 1,   le: 1UL <= 1,   gt: 1UL > 1,   ge: 1UL >= 1),
                    new BinaryOperatorTestEntry(1.0, 1UL, eq: false,      neq: true,       lt: 1   < 1UL, le: 1   <= 1UL, gt: 1   > 1UL, ge: 1   >= 1UL),
                    new BinaryOperatorTestEntry(1UL, 1L,  eq: 1UL == 1L,  neq: 1UL != 1L,  lt: 1UL < 1L,  le: 1UL <= 1L,  gt: 1UL > 1L,  ge: 1UL >= 1L),
                    new BinaryOperatorTestEntry(1L,  1UL, eq: 1L  == 1UL, neq: 1L  != 1UL, lt: 1L  < 1UL, le: 1L  <= 1UL, gt: 1L  > 1UL, ge: 1L  >= 1UL),

                    // overflows
                    new BinaryOperatorTestEntry(max, 1UL, add: (double)max + 1UL),
                    new BinaryOperatorTestEntry(max, 1.0, add: (double)max + 1),
                    new BinaryOperatorTestEntry(min, 1UL, sub: (double)min - 1UL),
                    new BinaryOperatorTestEntry(min, 1.0, sub: (double)min - 1),
                    new BinaryOperatorTestEntry(max, 2UL, mult: (double)max * 2UL),
                    new BinaryOperatorTestEntry(max, 2.0, mult: (double)max * 2),
                    new BinaryOperatorTestEntry(max, 0.5, div: (double)max / 0.5),

                    // range
                    new BinaryOperatorTestEntry(0L,   min,  eq: true,  neq: false, lt: false, le: true,  gt: false, ge: true),
                    new BinaryOperatorTestEntry(min,  0L,   eq: true,  neq: false, lt: false, le: true,  gt: false, ge: true),
                    new BinaryOperatorTestEntry(0L,   max,  eq: false, neq: true,  lt: true,  le: true,  gt: false, ge: false),
                    new BinaryOperatorTestEntry(max,  0L,   eq: false, neq: true,  lt: false, le: false, gt: true,  ge: true),
                    new BinaryOperatorTestEntry(0UL,  smin, eq: false, neq: true,  lt: false, le: false, gt: true,  ge: true),
                    new BinaryOperatorTestEntry(smin, 0UL,  eq: false, neq: true,  lt: true,  le: true,  gt: false, ge: false),
                    new BinaryOperatorTestEntry(0UL,  smax, eq: false, neq: true,  lt: true,  le: true,  gt: false, ge: false),
                    new BinaryOperatorTestEntry(smax, 0UL,  eq: false, neq: true,  lt: false, le: false, gt: true,  ge: true),
                    new BinaryOperatorTestEntry(smin, min,  eq: false, neq: true,  lt: true,  le: true,  gt: false, ge: false),
                    new BinaryOperatorTestEntry(min,  smin, eq: false, neq: true,  lt: false, le: false, gt: true,  ge: true),
                    new BinaryOperatorTestEntry(smax, max,  eq: false, neq: true,  lt: true,  le: true,  gt: false, ge: false),
                    new BinaryOperatorTestEntry(max,  smax, eq: false, neq: true,  lt: false, le: false, gt: true,  ge: true),
                    new BinaryOperatorTestEntry((Int64)min,   min,          eq: true, neq: false, lt: false, le: true, gt: false, ge: true),
                    new BinaryOperatorTestEntry(min,          (Int64)min,   eq: true, neq: false, lt: false, le: true, gt: false, ge: true),
                    new BinaryOperatorTestEntry((UInt64)smax, smax,         eq: true, neq: false, lt: false, le: true, gt: false, ge: true),
                    new BinaryOperatorTestEntry(smax,         (UInt64)smax, eq: true, neq: false, lt: false, le: true, gt: false, ge: true),
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
                    new Tuple<object, object>(0UL, -(double)0UL),
                    new Tuple<object, object>(min, -(double)min),
                    new Tuple<object, object>(max, -(double)max),
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
