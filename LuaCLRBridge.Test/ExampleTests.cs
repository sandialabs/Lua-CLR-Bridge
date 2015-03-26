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
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_1_1()
        {
            using (var lua = new LuaBridge())
            {
                var r = lua.Do("return " + "CLR.Type['System.Int32']");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(typeof(System.Int32), r[0]);
            }
        }
    }

    namespace Example_1_2
    {
        namespace N
        {
            public delegate int F();
            public delegate void G( int i );
            public delegate int H( out string s, int i );
            public delegate void I( string s, ref int i );
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_1_2_a()
        {
            using (var lua = new LuaBridge())
            {
                var r = lua.Do(@"local dF, dG, dH, dI
                                 dF = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.F'],
                                                      function( ) local r = 0 return r end)
                                 dG = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.G'],
                                                      function( i ) return end)
                                 dH = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.H'],
                                                      function( i ) local r, s = 0, 'hello'
                                                                    return r, s end)
                                 dI = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.I'],
                                                     function( s, i ) i = i + 1 return i end)" + "; return dF, dG, dH, dI");

                Assert.AreEqual(4, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Example_1_2.N.F));
                Assert.IsInstanceOfType(r[1], typeof(Example_1_2.N.G));
                Assert.IsInstanceOfType(r[2], typeof(Example_1_2.N.H));
                Assert.IsInstanceOfType(r[3], typeof(Example_1_2.N.I));
                int v = (r[0] as Example_1_2.N.F)();
                (r[1] as Example_1_2.N.G)(0);
                string s;
                v = (r[2] as Example_1_2.N.H)(out s, 0);
                int i = 0;
                (r[3] as Example_1_2.N.I)(s, ref i);
            }
        }

        class C_1_2_b
        {
            public int F() { /*…*/ return 0; }
            public void F( int i ) { /*…*/ }
        }

        [TestMethod]
        public void TestExample_1_2_b()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_1_2_b();

                lua["c"] = c;

                var r = lua.Do(@"local dF, dG
                                 dF = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.F'], c.F)
                                 dG = CLR.NewDelegate(CLR.Type['LuaCLRBridge.Test.Example_1_2.N.G'], c.F)" + "; return dF, dG");

                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(new Example_1_2.N.F(c.F), r[0]);
                Assert.AreEqual(new Example_1_2.N.G(c.F), r[1]);
            }
        }

        [TestMethod]
        public void TestExample_1_4_a()
        {
            using (var lua = new LuaBridge())
            {
                var a = new object[] { new object() };

                lua["a"] = a;

                var r = lua.Do("local value = a[0]" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(a[0], r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("a[0] = value");

                Assert.AreSame(value, a[0]);
            }
        }

        [TestMethod]
        public void TestExample_1_4_b()
        {
            using (var lua = new LuaBridge())
            {
                var a = new object[,] { { new object() } };

                lua["a"] = a;

                var r = lua.Do("local value = a[{0, 0}]" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(a[0, 0], r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("a[{0, 0}] = value");

                Assert.AreSame(value, a[0, 0]);
            }
        }

        private class C_2_1
        {
            private Func<int> _delegate;

            public event Func<int> E
            {
                add { _delegate += value; }
                remove { _delegate -= value; }
            }
        }

        [TestMethod]
        public void TestExample_2_1()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_1();

                lua["c"] = c;

                lua.Do("f = function (i) print(i) end");

                lua.Do("CLR.AddHandler(c.E, f)");

                var _delegate = new PrivateObject(c).GetField("_delegate") as Func<int>;
                Assert.IsNotNull(_delegate);
                Assert.AreEqual(1, _delegate.GetInvocationList().Length);

                var value = new object();

                lua.Do("CLR.RemoveHandler(c.E, f)");

                _delegate = new PrivateObject(c).GetField("_delegate") as Func<int>;
                Assert.IsNull(_delegate);
            }
        }

        private class C_2_2
        {
            public object X;
        }

        [TestMethod]
        public void TestExample_2_2()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_2();
                c.X = new object();

                lua["c"] = c;

                var r = lua.Do("local value = c.X" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(c.X, r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("c.X = value");

                Assert.AreSame(value, c.X);
            }
        }

        private class C_2_3
        {
            internal string called { get; private set; }

            public void F() { called = "F()"; }
            public void F( int i ) { called = "F(int)"; }
            public string F( string s ) { called = "F(string)"; return ""; }
            public void G( int i = 0 ) { called = "G(int)"; }
            public int G( out int[] a ) { called = "G(out int[])"; a = new int[0]; return 0; }
            public void H( ref int i ) { called = "H(ref int)"; }
            public void H( params int[] a ) { called = "H(params int[])"; }
        }

        [TestMethod]
        public void TestExample_2_3()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_3();

                lua["c"] = c;

                var test = new C_2_3();

                var ret = lua.Do("c.F()");
                test.F();

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);

                ret = lua.Do("c.F(1)");
                test.F(1);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);

                ret = lua.Do("local ret = c.F('hello')" + "; return ret");
                object test_ret = test.F("hello");

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(1, ret.Length);
                Assert.AreEqual(test_ret, ret[0]);

                ret = lua.Do("c.G()");
                test.G();

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);

                ret = lua.Do("c.G(1)");
                test.G(1);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);

                ret = lua.Do("local ret, out = c.G(nil)" + "; return ret, out");
                int[] test_out;
                test_ret = test.G(out test_out);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(2, ret.Length);
                Assert.AreEqual((double)(int)test_ret, ret[0]);
                Assert.AreEqual(test_out.Length, (ret[1] as int[]).Length);

                ret = lua.Do("local ref = c.H(1)" + "; return ref");
                int test_ref = 1;
                test.H(ref test_ref);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(1, ret.Length);
                Assert.AreEqual((double)(int)test_ref, ret[0]);

                ret = lua.Do("c.H()");
                test.H();

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);

                ret = lua.Do("c.H(1, 2, 3)");
                test.H(1, 2, 3);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, ret.Length);
            }
        }

        private class C_2_4_a
        {
            public object X { get; set; }
        }

        [TestMethod]
        public void TestExample_2_4_a()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_4_a();
                c.X = new object();

                lua["c"] = c;

                var r = lua.Do("local value = c.X" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(c.X, r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("c.X = value");

                Assert.AreSame(value, c.X);
            }
        }

        private class C_2_4_b
        {
            private Dictionary<object, object> dictionary = new Dictionary<object, object>();
            public object this[ object i ] { get { return dictionary[i]; } set { dictionary[i] = value; } }
        }

        [TestMethod]
        public void TestExample_2_4_b()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_4_b();
                var x = new object();
                c[x] = new object();

                lua["c"] = c;
                lua["x"] = x;

                var r = lua.Do("local value = c.Item[x]" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(c[x], r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("c.Item[x] = value");

                Assert.AreSame(value, c[x]);
            }
        }
    }

    namespace Example_2_5
    {
        namespace N
        {
            public class C
            {
                public static object X;
            }
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_2_5()
        {
            using (var lua = new LuaBridge())
            {
                Example_2_5.N.C.X = new object();

                var r = lua.Do("local value = CLR.Static['LuaCLRBridge.Test.Example_2_5.N.C'].X" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(Example_2_5.N.C.X, r[0]);
            }
        }
    }

    namespace Example_2_6
    {
        namespace N
        {
            public class C
            {
                public C( int i )
                {
                }
            }
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_2_6()
        {
            using (var lua = new LuaBridge())
            {
                var r = lua.Do(@"local C = CLR.Static['LuaCLRBridge.Test.Example_2_6.N.C']
                                 local c = C(1)" + "; return c");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Example_2_6.N.C));
            }
        }
    }

    namespace Example_2_7
    {
        namespace N
        {
            public class C
            {
                public class T
                {
                    public static object X;
                }
            }
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_2_7()
        {
            using (var lua = new LuaBridge())
            {
                Example_2_5.N.C.X = new object();

                var r = lua.Do("local value = CLR.Static['LuaCLRBridge.Test.Example_2_7.N.C'].T.X" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(Example_2_7.N.C.T.X, r[0]);
            }
        }

        private class C_2_8
        {
            internal static C_2_8 lhs, rhs, result;
            public static C_2_8 operator +( C_2_8 lhs, C_2_8 rhs ) { C_2_8.lhs = lhs; C_2_8.rhs = rhs; return result; }
        }

        [TestMethod]
        public void TestExample_2_8()
        {
            using (var lua = new LuaBridge())
            {
                var c1 = new C_2_8();
                var c2 = new C_2_8();
                lua["c1"] = c1;
                lua["c2"] = c2;
                C_2_8.result = new C_2_8();

                var r = lua.Do("local value = c1 + c2" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(C_2_8.result, r[0]);
                Assert.AreSame(C_2_8.lhs, c1);
                Assert.AreSame(C_2_8.rhs, c2);
            }
        }

        private class C_2_8_1
        {
            public static bool operator ==( C_2_8_1 lhs, C_2_8_1 rhs )
            {
                return false;
            }

            public static bool operator !=( C_2_8_1 lhs, C_2_8_1 rhs )
            {
                return !(lhs == rhs);
            }

            public override int GetHashCode() { return base.GetHashCode(); }

            public override bool Equals( object obj ) { return base.Equals(obj); }
        }

        [TestMethod]
        public void TestExample_2_8_1()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_8_1();
                lua["c"] = c;

                var r = lua.Do(@"local d = c
                                 local eq = c == d" + "; return eq");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }

        [TestMethod]
        public void TestExample_2_9_1()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_4_b();
                var x = new object();
                c[x] = new object();

                lua["c"] = c;
                lua["x"] = x;

                var r = lua.Do("local value = c{SpecialName = true}.get_Item(x)" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreSame(c[x], r[0]);

                var value = new object();

                lua["value"] = value;

                lua.Do("c{SpecialName = true}.set_Item(x, value)");

                Assert.AreSame(value, c[x]);
            }
        }

        private class C_2_10_a
        {
            internal string called { get; private set; }

            public void F( int l, double r )
            {
                called = "F(int,double)";
            }

            public void F( double l, int r )
            {
                called = "F(double,int)";
            }
        }


        [TestMethod]
        public void TestExample_2_10_a()
        {
            using (var lua = new LuaBridge())
            {
                var c = new C_2_10_a();

                lua["c"] = c;

                var test = new C_2_10_a();

                var r = lua.Do("c.F{'Int32', 'Double'}(1, 2)");
                test.F(1, 2.0);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, r.Length);

                r = lua.Do(@"c.F{CLR.Type['System.Double'],
                                 CLR.Type['System.Int32']}(1, 2)");
                test.F(1.0, 2);

                Assert.AreEqual(c.called, test.called);
                Assert.AreEqual(0, r.Length);
            }
        }
    }

    namespace Example_2_10
    {
        namespace N
        {
            public class C
            {
                internal LuaTable table { get; private set; }

                public C( LuaTable t ) { table = t; }
                public object this[object i] { get { return table[i]; } }
            }
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_2_10_b()
        {
            using (var lua = new LuaBridge())
            {
                var r = lua.Do(@"local C = CLR.Static['LuaCLRBridge.Test.Example_2_10.N.C']
                                 local value = C{}{}({ x = true }){SpecialName = true}.get_Item('x')" + "; return value");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }
    }

    namespace Example_3_0
    {
        namespace N
        {
            public class C<T, U>
            {
                public C() { /*…*/ }
            }
        }
    }

    public partial class ExampleTests
    {
        [TestMethod]
        public void TestExample_3_0_a()
        {
            using (var lua = new LuaBridge())
            {
                var r = lua.Do(@"local C = CLR.Static['LuaCLRBridge.Test.Example_3_0.N.C`2']
                                 local c = C{CLR.Type['System.Int32'],
                                             CLR.Type['System.Double']}()" + "; return c");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Example_3_0.N.C<int, double>));
            }
        }
    }
}
