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
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MethodResolutionGenericTests : SandboxTestsBase
    {
        [Serializable]
        private class IEnumerableMethod
        {
            public double f( IEnumerable<double> a ) { foreach (double d in a) return d; return 0; }
        }

        [TestMethod]
        public void CallIEnumerableMethodArrayParameter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new IEnumerableMethod();
                var a = new double[] { 1 };

                lua["x"] = x;
                lua["a"] = a;

                var r = lua.Do("return x.f(a)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(a[0], r[0]);
            }
        }

        [Serializable]
        private class Base
        {
            public override bool Equals( object obj ) { return obj != null && obj.GetType() == this.GetType(); }
            public override int GetHashCode() { return this.GetType().GetHashCode(); }
        }

        [Serializable]
        private class Derived : Base
        {
        }

        [Serializable]
        private class SiblingDerived : Base
        {
        }

        [Serializable]
        private class Unrelated
        {
        }

        [Serializable]
        private class GenericBasicMethods
        {
            public string i1<T>( T x ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, x); }
            public string i1<T, U>( T x, U y ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}, {1}>({2}, {3})", typeof(T).Name, typeof(U).Name, x, y); }
            public string i1<T, U>( T x, T y, U z ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}, {1}>({2}, {3}, {4})", typeof(T).Name, typeof(U).Name, x, y, z); }

            public string i2<T>( object x, T y ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T).Name, x, y); }
        }

        [TestMethod]
        public void CallGenericBasicMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericBasicMethods();
                var b = new Base();
                var u = new Unrelated();

                lua["g"] = g;
                lua["b"] = b;
                lua["u"] = u;

                var r = lua.Do("return g.i1(b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b), r[0]);

                r = lua.Do("return g.i1(u)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(u), r[0]);

                r = lua.Do("return g.i1(b, b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b, b), r[0]);

                r = lua.Do("return g.i1(b, u)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b, u), r[0]);

                r = lua.Do("return g.i1(b, b, u)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b, b, u), r[0]);

                try
                {
                    r = lua.Do("return g.i1(b, u, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.i2(b, b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i2(b, b), r[0]);
            }
        }

        [Serializable]
        private class GenericCovariantParameterTypeMethods
        {
            public string i1<T>( T x, T y ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T).Name, x, y); }
        }

        [Serializable]
        private class GenericContravriantParameterTypeMethods
        {
            public string o1<T>( out T x, out T y ) { x = default(T); y = default(T); return MethodBase.GetCurrentMethod().MethodFormat("<{0}>(out {1}, out {2})", typeof(T).Name, x, y); }
        }

        [Serializable]
        private class GenericCovariantContravariantParameterTypeMethods
        {
            public string io1<T>( T x, out T y ) { y = x; return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, out {2})", typeof(T).Name, x, y); }
        }

        [Serializable]
        private class GenericCovariantInvariantParameterTypesMethods
        {
            public string ir1<T>( T x, ref T y ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, ref {2})", typeof(T).Name, x, y); }
        }

        [TestMethod]
        public void CallGenericCovariantParameterTypeMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericCovariantParameterTypeMethods();
                var b = new Base();
                var d = new Derived();
                var s = new SiblingDerived();
                var u = new Unrelated();

                lua["g"] = g;
                lua["b"] = b;
                lua["d"] = d;
                lua["s"] = s;
                lua["u"] = u;

                var r = lua.Do("return g.i1(b, b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b, b), r[0]);

                r = lua.Do("return g.i1(b, d)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(b, d), r[0]);

                try
                {
                    r = lua.Do("return g.i1(b, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.i1(d, b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(d, b), r[0]);

                try
                {
                    r = lua.Do("return g.i1(d, s)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallGenericContravriantParameterTypeMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericContravriantParameterTypeMethods();
                var b = new Base();
                var d = new Derived();
                var s = new SiblingDerived();
                var u = new Unrelated();

                lua["g"] = g;
                lua["b"] = b;
                lua["d"] = d;
                lua["s"] = s;
                lua["u"] = u;

                var r = lua.Do("return g.o1(b, b)");

                Assert.AreEqual(3, r.Length);
                Assert.AreEqual(g.o1(out b, out b), r[0]);

                try
                {
                    // type inference doesn't fail here, but the CIL
                    // sees "out" the same as "ref", so resolution fails
                    r = lua.Do("return g.o1(b, d)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                try
                {
                    r = lua.Do("return g.o1(b, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }


                try
                {
                    // type inference doesn't fail here, but the CIL
                    // sees "out" the same as "ref", so resolution fails
                    r = lua.Do("return g.o1(d, b)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                try
                {
                    r = lua.Do("return g.o1(d, s)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallGenericCovariantContravariantParameterTypeMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericCovariantContravariantParameterTypeMethods();
                var b = new Base();
                var d = new Derived();
                var s = new SiblingDerived();
                var u = new Unrelated();

                lua["g"] = g;
                lua["b"] = b;
                lua["d"] = d;
                lua["s"] = s;
                lua["u"] = u;

                object[] r;

                r = lua.Do("return g.io1(b, b)");

                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(g.io1(b, out b), r[0]);
                Assert.AreEqual(b, r[1]);

                try
                {
                    r = lua.Do("return g.io1(b, d)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                try
                {
                    r = lua.Do("return g.io1(b, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.io1(d, b)");

                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(g.io1(d, out b), r[0]);
                Assert.AreEqual(b, r[1]);

                try
                {
                    r = lua.Do("return g.io1(d, s)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallGenericCovariantInvariantParameterTypesMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericCovariantInvariantParameterTypesMethods();
                var b = new Base();
                var d = new Derived();
                var s = new SiblingDerived();
                var u = new Unrelated();

                lua["g"] = g;
                lua["b"] = b;
                lua["d"] = d;
                lua["s"] = s;
                lua["u"] = u;

                object[] r;

                r = lua.Do("return g.ir1(b, b)");

                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(g.ir1(b, ref b), r[0]);
                Assert.AreEqual(b, r[1]);

                try
                {
                    r = lua.Do("return g.ir1(b, d)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                try
                {
                    r = lua.Do("return g.ir1(b, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.ir1(d, b)");

                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(g.ir1(d, ref b), r[0]);
                Assert.AreEqual(b, r[1]);

                try
                {
                    r = lua.Do("return g.ir1(d, s)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        [Serializable]
        private class GenericParameterTypeContravariantTypeArgsMethods
        {
            public string o1<T>( Func<T> f ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, f); }
        }

        [Serializable]
        private class GenericParameterTypeCovariantTypeArgsMethods
        {
            public string i1<T>( Action<T> f ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, f); }
        }

        [Serializable]
        private class GenericParameterTypeInvariantTypeArgsMethods
        {
            public string r1<T>( List<T> l ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, l); }
        }

        [Serializable]
        private class GenericParameterTypeCovariantContravariantTypeArgsMethods
        {
            public string oi1<T>( Func<T, T> f ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, f); }
        }

        [TestMethod]
        public void CallGenericParameterTypeContravariantTypeArgsMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericParameterTypeContravariantTypeArgsMethods();
                var d = new Derived();
                var fb = new Func<Base>(() => null);

                lua["g"] = g;
                lua["fb"] = fb;

                var r = lua.Do("return g.o1(fb)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.o1(fb), r[0]);
            }
        }

        [TestMethod]
        public void CallGenericParameterTypeCovariantTypeArgsMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericParameterTypeCovariantTypeArgsMethods();
                var ab = new Action<Base>(( a ) => { });

                lua["g"] = g;
                lua["ab"] = ab;

                var r = lua.Do("return g.i1(ab)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(ab), r[0]);
            }
        }

        [TestMethod]
        public void CallGenericParameterTypeInvariantTypeArgsMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericParameterTypeInvariantTypeArgsMethods();
                var lb = new List<Base>();

                lua["g"] = g;
                lua["lb"] = lb;

                var r = lua.Do("return g.r1(lb)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.r1(lb), r[0]);
            }
        }

        [TestMethod]
        public void CallGenericParameterTypeCovariantContravariantTypeArgsMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericParameterTypeCovariantContravariantTypeArgsMethods();
                var fbb = new Func<Base, Base>(( v ) => null);
                var fbd = new Func<Base, Derived>(( v ) => null);
                var fdb = new Func<Derived, Base>(( v ) => null);
                var fdd = new Func<Derived, Derived>(( v ) => null);

                lua["g"] = g;
                lua["fbb"] = fbb;
                lua["fbd"] = fbd;
                lua["fdb"] = fdb;
                lua["fdd"] = fdd;

                var r = lua.Do("return g.oi1(fbb)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.oi1(fbb), r[0]);

                r = lua.Do("return g.oi1(fbd)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.oi1(fbd), r[0]);

                try
                {
                    r = lua.Do("return g.oi1(fdb)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.oi1(fdd)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.oi1(fdd), r[0]);
            }
        }

        [Serializable]
        private class GenericMethods
        {
            // covariant parameter
            public string i1<T>( T t ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), t); }
        }

        [TestMethod]
        public void CallGenericMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericMethods();
                double d = 1;
                object n = null;

                lua["g"] = g;
                lua["d"] = d;
                lua["n"] = n;

                var r = lua.Do("return g.i1(d)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(d), r[0]);

                r = lua.Do("return g.i1(n)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(n), r[0]);
            }
        }

        [Serializable]
        private class GenericArrayArgsMethods
        {
            // covariant parameters
            public string i1<T>( T[] ts ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ts); }
            public string i1<T>( T[] tsa, T[] tsb ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), tsa, tsb); }
        }

        [TestMethod]
        public void CallGenericParameterTypeCovariantArrayMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericArrayArgsMethods();
                var ab = new Base[0];
                var ad = new Derived[1];
                var @as = new SiblingDerived[2];
                var au = new Unrelated[3];

                lua["g"] = g;
                lua["ab"] = ab;
                lua["ad"] = ad;
                lua["as"] = @as;
                lua["au"] = au;

                var r = lua.Do("return g.i1(ab)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(ab), r[0]);

                r = lua.Do("return g.i1(ab, ab)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(ab, ab), r[0]);

                r = lua.Do("return g.i1(ab, ad)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.i1(ab, ad), r[0]);

                try
                {
                    r = lua.Do("return g.i1(ab, au)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                try
                {
                    r = lua.Do("return g.i1(ad, as)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        [Serializable]
        private class GenericParamsMethod
        {
            // covariant parameters
            public int i1<T>( params T[] ts ) { return ts.Length; }
        }

        [TestMethod]
        public void CallGenericParamsMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericParamsMethod();
                var b = new Base();
                var d = new Derived();
                var s = new SiblingDerived();
                var u = new Unrelated();
                var ab = new Base[0];

                lua["g"] = g;
                lua["b"] = b;
                lua["d"] = d;
                lua["s"] = s;
                lua["u"] = u;
                lua["ab"] = ab;

                var r = lua.Do("return g.i1(b)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)g.i1(b), r[0]);

                try
                {
                    r = lua.Do("return g.i1(b, u)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.i1(b, d)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)g.i1(b, d), r[0]);

                try
                {
                    r = lua.Do("return g.i1(d, s)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.i1(ab)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)g.i1(ab), r[0]);
            }
        }

        [Serializable]
        private class GenericRecursiveInference
        {
            public void f<T>( List<Func<T, T>> l ) { }
            public void g<T, U>( List<Func<T, U>> l ) { }
        }

        [TestMethod]
        public void CallGenericRecursiveInference()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericRecursiveInference();
                var lfdd = new List<Func<Double, Double>>();
                var lfds = new List<Func<Double, String>>();

                lua["g"] = g;
                lua["lfdd"] = lfdd;
                lua["lfds"] = lfds;

                var r = lua.Do("return g.f(lfdd)");

                Assert.AreEqual(0, r.Length);

                try
                {
                    r = lua.Do("return g.f(lfds)");

                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.g(lfdd)");

                Assert.AreEqual(0, r.Length);

                r = lua.Do("return g.g(lfds)");

                Assert.AreEqual(0, r.Length);
            }
        }

        [Serializable]
        private class GenericNonGenericOverloadedMethods
        {
            public string f<T>( T x ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T).Name, x); }
            public string f( int x ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", x); }
        }

        [TestMethod]
        public void CallGenericNonGenericOverloadedMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericNonGenericOverloadedMethods();
                var x = 1;
                var y = "y";

                lua["g"] = g;
                lua["x"] = x;
                lua["y"] = y;

                var r = lua.Do("return g.f(x)");

                Assert.AreEqual(1, r.Length);
                // Assert.AreEqual(g.f(x), r[0]);
                // TODO: LuaBinder type inference infers T = double which may not be what we want
                Assert.AreEqual(g.f<double>(x), r[0]);

                r = lua.Do("return g.f(y)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.f(y), r[0]);
            }
        }

        [Serializable]
        private class GenericNoninferribleMethods
        {
            public string f<T>() { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>()", typeof(T).Name); }
            public string f<U, V>( V v ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}, {1}>({2})", typeof(U).Name, typeof(V).Name, v); }
        }

        [TestMethod]
        public void CallGenericNullaryMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var g = new GenericNoninferribleMethods();

                lua["g"] = g;

                var r = lua.Do("return g.f{_ = {g.GetType()}}()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.f<GenericNoninferribleMethods>(), r[0]);

                try
                {
                    r = lua.Do("return g.f()");
                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }

                r = lua.Do("return g.f{_ = {g.GetType(), CLR.Type['System.String']}}('test')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(g.f<GenericNoninferribleMethods, string>("test"), r[0]);

                try
                {
                    r = lua.Do("return g.f{_ = {g.GetType()}}('test')");
                    Assert.Fail();
                }
                catch (MissingMethodException)
                {
                    // expected
                }
            }
        }

        // TODO: test passing nonsense hints
    }
}
