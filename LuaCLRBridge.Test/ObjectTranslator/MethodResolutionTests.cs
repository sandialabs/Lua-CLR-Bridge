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
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MethodResolutionTests : SandboxTestsBase
    {
        private class Box : MarshalByRefObject
        {
            public object Value { get; set; }
        }

        [Serializable]
        private class VoidMethod
        {
            internal readonly Box box = new Box();

            public void f() { box.Value = MethodInfo.GetCurrentMethod(); }
        }

        [TestMethod]
        public void CallVoidMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new VoidMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f()");

                Assert.AreEqual(0, r.Length);

                var expected = new VoidMethod();
                expected.f();

                Assert.AreEqual(expected.box.Value, x.box.Value);
            }
        }

        [Serializable]
        private class SixtyFourBitIntegerMethod
        {
            public string f( Int64 i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
            public string f( UInt64 i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

            public string g( double d ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", d); }
        }

        [TestMethod]
        public void CallSixtyFourBitIntegerMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new SixtyFourBitIntegerMethod();
                var y = 0L;
                var z = 0UL;

                lua["x"] = x;
                lua["y"] = y;
                lua["z"] = z;

                var r = lua.Do("return x.f(y)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(y), r[0]);

                r = lua.Do("return x.f(z)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(z), r[0]);

                try
                {
                    r = lua.Do("return x.g(y)");

                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'g(System.Int64)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+SixtyFourBitIntegerMethod'"));
                }

                try
                {
                    r = lua.Do("return x.g(z)");

                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'g(System.UInt64)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+SixtyFourBitIntegerMethod'"));
                }
            }
        }

        [Serializable]
        private class MismatchedMethod
        {
            public string f() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }
            public string g( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
        }

        [TestMethod]
        public void CallMismatchedMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MismatchedMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(), r[0]);

                try
                {
                    r = lua.Do("return x.f(1)");
                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'f(System.Double)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+MismatchedMethod'"));
                }

                r = lua.Do("return x.g(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g(1), r[0]);

                try
                {
                    r = lua.Do("return x.g()");
                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'g()' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+MismatchedMethod'"));
                }
            }
        }

        [TestMethod]
        public void CallMissingMemberNullArgument()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MismatchedMethod();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x.f(null)");
                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'f(null)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+MismatchedMethod'"));
                }
            }
        }

        [TestMethod]
        public void CallNullValueTypeArgument()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new MismatchedMethod();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x.g(null)");
                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'g(null)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+MismatchedMethod'"));
                }
            }
        }

        [Serializable]
        private class ValueParamsMethod
        {
            public string f( params int[] x ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { x }); }
        }

        [TestMethod]
        public void CallParamsMethodExtended()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ValueParamsMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f(1, 2, 3)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1, 2, 3), r[0]);
            }
        }

        [TestMethod]
        public void CallParamsMethodNormal()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ValueParamsMethod();

                lua["x"] = x;
                lua["y"] = new int[] { 1, 2, 3 };

                var r = lua.Do("return x.f(y)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1, 2, 3), r[0]);
            }
        }

        [Serializable]
        private class ParamsMethodNull
        {
            public string f( params object[] x ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { x }); }
        }

        [TestMethod]
        public void CallParamsMethodNull()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ParamsMethodNull();

                lua["x"] = x;

                var r = lua.Do("return x.f(null)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(null), r[0]);
            }
        }

        [Serializable]
        private abstract class OptionalAndParamsMethodsBase
        {
            public abstract object[] expected { get; }

            protected readonly Type missing = typeof(MissingMemberException);

            [Serializable]
            public class Int : OptionalAndParamsMethodsBase
            {
                public string f( int x ) { return "f1/" + x; }
                public override object[] expected { get { return new object[] { missing, "f1/1", missing, missing }; } }
            }

            [Serializable]
            public class IntInt : OptionalAndParamsMethodsBase
            {
                public string f( int x, int y ) { return "f2/" + x + "/" + y; }
                public override object[] expected { get { return new object[] { missing, missing, "f2/1/2", missing }; } }
            }

            [Serializable]
            public class OptionalInt : OptionalAndParamsMethodsBase
            {
                public string f( int x = -1 ) { return "f0-1/" + x; }
                public override object[] expected { get { return new object[] { "f0-1/-1", "f0-1/1", missing, missing }; } }
            }

            [Serializable]
            public class IntOptionalInt : OptionalAndParamsMethodsBase
            {
                public string f( int x, int y = -2 ) { return "f1-2/" + x + "/" + y; }
                public override object[] expected { get { return new object[] { missing, "f1-2/1/-2", "f1-2/1/2", missing }; } }
            }

            [Serializable]
            public class OptionalIntOptionalInt : OptionalAndParamsMethodsBase
            {
                public string f( int x = -1, int y = -2 ) { return "f0-2/" + x + "/" + y; }
                public override object[] expected { get { return new object[] { "f0-2/-1/-2", "f0-2/1/-2", "f0-2/1/2", missing }; } }
            }

            [Serializable]
            public class IntOptionalIntOptionalInt : OptionalAndParamsMethodsBase
            {
                public string f( int x, int y = -2, int z = -3 ) { return "f1-3/" + x + "/" + y + "/" + z; }
                public override object[] expected { get { return new object[] { missing, "f1-3/1/-2/-3", "f1-3/1/2/-3", "f1-3/1/2/3" }; } }
            }

            [Serializable]
            public class ParamsInt : OptionalAndParamsMethodsBase
            {
                public string f( params int[] x ) { return "f0-n/" + x.Length; }
                public override object[] expected { get { return new object[] { "f0-n/0", "f0-n/1", "f0-n/2", "f0-n/3" }; } }
            }

            [Serializable]
            public class IntParamsInt : OptionalAndParamsMethodsBase
            {
                public string f( int x, params int[] y ) { return "f1-n/" + x + "/" + y.Length; }
                public override object[] expected { get { return new object[] { missing, "f1-n/1/0", "f1-n/1/1", "f1-n/1/2" }; } }
            }

            [Serializable]
            public class OptionalIntParamsInt : OptionalAndParamsMethodsBase
            {
                public string f( int x = -1, params int[] y ) { return "f0-n/" + x + "/" + y.Length; }
                public override object[] expected { get { return new object[] { "f0-n/-1/0", "f0-n/1/0", "f0-n/1/1", "f0-n/1/2" }; } }
            }

            [Serializable]
            public class IntOptionalIntParamsInt : OptionalAndParamsMethodsBase
            {
                public string f( int x, int y = -2, params int[] z ) { return "f1-n/" + x + "/" + y + "/" + z.Length; }
                public override object[] expected { get { return new object[] { missing, "f1-n/1/-2/0", "f1-n/1/2/0", "f1-n/1/2/1" }; } }
            }
        }

        [TestMethod]
        public void CallOptionalAndParamsMethod()
        {
            var xs = new OptionalAndParamsMethodsBase[]
            {
                new OptionalAndParamsMethodsBase.Int(),
                new OptionalAndParamsMethodsBase.IntInt(),
                new OptionalAndParamsMethodsBase.OptionalInt(),
                new OptionalAndParamsMethodsBase.IntOptionalInt(),
                new OptionalAndParamsMethodsBase.OptionalIntOptionalInt(),
                new OptionalAndParamsMethodsBase.IntOptionalIntOptionalInt(),
                new OptionalAndParamsMethodsBase.ParamsInt(),
                new OptionalAndParamsMethodsBase.IntParamsInt(),
                new OptionalAndParamsMethodsBase.OptionalIntParamsInt(),
                new OptionalAndParamsMethodsBase.IntOptionalIntParamsInt(),
            };

            using (var lua = CreateLuaBridge())
            {
                foreach (OptionalAndParamsMethodsBase x in xs)
                {
                    lua["x"] = x;

                    for (int i = 0; i < 4; ++ i)
                    {
                        object expected = x.expected[i];

                        object[] r = null;

                        try
                        {
                            r = lua.Do(i == 0 ?
                                "return x.f()" : i == 1 ?
                                "return x.f(1)" : i == 2 ?
                                "return x.f(1, 2)" : i == 3 ?
                                "return x.f(1, 2, 3)" : "error('unexpected')");

                            Assert.AreEqual(1, r.Length, x.GetType().ToString());
                            Assert.AreEqual(expected as string, r[0], x.GetType().ToString());
                        }
                        catch (Exception ex)
                        {
                            if (expected is Type && (ex is AmbiguousMatchException || ex is MissingMemberException))
                                Assert.IsInstanceOfType(ex, expected as Type, x.GetType().ToString());
                            else
                                throw;
                        }
                    }
                }
            }
        }

        [Serializable]
        private class ByValByRefOverloadedMethods
        {
            public string f( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
            public string f( ref int i ) { return MethodBase.GetCurrentMethod().MethodFormat("(ref {0})", i); }
        }

        [TestMethod]
        public void CallByValByRefOverloadedMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new ByValByRefOverloadedMethods();

                lua["x"] = x;

                var r = lua.Do("return x.f{'Int32'}(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);

                r = lua.Do("return x.f{CLR.Type['System.Int32']}(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);

                r = lua.Do("return x.f{'Int32&'}(1)");

                Assert.AreEqual(2, r.Length);
                int i = 1;
                Assert.AreEqual(x.f(ref i), r[0]);
                Assert.AreEqual((double)i, r[1]);

                r = lua.Do("return x.f{CLR.Type['System.Int32'].MakeByRefType()}(1)");

                Assert.AreEqual(2, r.Length);
                i = 1;
                Assert.AreEqual(x.f(ref i), r[0]);
                Assert.AreEqual((double)i, r[1]);
            }
        }

        [Serializable]
        private class AmbiguousOverloadedMethods
        {
            public string f( IComparable i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
            public string f( IFormattable i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
        }

        [TestMethod]
        public void CallAmbiguousOverloadedMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new AmbiguousOverloadedMethods();

                lua["x"] = x;

                var r = lua.Do("return x.f{'IComparable'}(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f((IComparable)1), r[0]);

                r = lua.Do("return x.f{CLR.Type['System.IFormattable']}(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f((IFormattable)1), r[0]);

                try
                {
                    r = lua.Do("return x.f(1)");
                    Assert.Fail();
                }
                catch (AmbiguousMatchException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallEmptyOverloadedMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new AmbiguousOverloadedMethods();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x.f{'IConvertible'}(1)");
                    Assert.Fail();
                }
                catch (MissingMethodException ex)
                {
                    // expected
                    Assert.IsTrue(ex.Message.StartsWith("'f(System.Double)' is not a member of type '"));
                    Assert.IsTrue(ex.Message.EndsWith("+AmbiguousOverloadedMethods'"));
                }
            }
        }

        [Serializable]
        private class IncompatibleOverloadedMethods
        {
            public string f( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
            public string f( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
        }

        [TestMethod]
        public void CallIncompatibleOverloadedMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new IncompatibleOverloadedMethods();

                lua["x"] = x;

                var r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);

                r = lua.Do("return x.f('1')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f("1"), r[0]);
            }
        }

        private static class BetterOverloadedMethods
        {
            [Serializable]
            public class ImplicitConversions
            {
                // better implicit conversions is better
                // see also BetterConversionFromTypeOverloadedMethods

                // implicit conversions are monotonically better

                public string f1( object o1, object o2 ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", o1, o2); }
                public string f1( string s1, string s2 ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s1, s2); }

                public string f2( string s, object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s, o); }
                public string f2( string s1, string s2 ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s1, s2); }

                public string f3( string s1, string s2 ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s1, s2); }
                public string f3( object o, string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", o, s); }

                public string f4( string s, object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s, o); }
                public string f4( object o, string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", o, s); }
            }

            [Serializable]
            public class Generic
            {
                // non-generic is better
                public string f1<T>( T o ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), o); }
                public string f1( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            }

            [Serializable]
            public class ParamArray
            {
                // normal/non-expanded form is better
                public string f1( string s, params string[] ss ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s, ss); }
                public string f1( string s1, string s2 ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s1, s2); }

                // more declared parameters is better
                public string g1( string s, params string[] ss ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s, ss.Length); }
                public string g1( string s1, string s2, params string[] ss ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1}, {2})", s1, s2, ss); }
            }

            [Serializable]
            public class DefaultParams
            {
                // exact number of arguments is better

                public string f1( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
                public string f1( string s1, string s2 = "" ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s1, s2); }

                public string f2( string s, object o = null ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1})", s, o); }
                public string f2( string s1, string s2 = "", string s3 = "" ) { return MethodBase.GetCurrentMethod().MethodFormat("({0}, {1}, {2})", s1, s2, s3); }
            }

            [Serializable]
            public class MoreSpecificParamTypes
            {
                // more specific parameter types is better

                public string f1<T>( T t ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), t); }
                public string f1<T>( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), s); }

                public string f2<T>( IEnumerable<T> iet ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), iet); }
                public string f2<T>( IEnumerable<string> ies ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ies); }

                public string f3<T>( IEnumerable<T[]> ieat ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ieat); }
                public string f3<T>( IEnumerable<string[]> ieas ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ieas); }

                public string f4<T>( T[] at ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), at); }
                public string f4<T>( string[] @as ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), @as); }

                public string f5<T>( IEnumerable<T>[] aiet ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), aiet); }
                public string f5<T>( IEnumerable<string>[] aies ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), aies); }

                // parameter types are monotonically more specific

                public string g1<T>( T t1, T t2 ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t1, t2); }
                public string g1<T>( string s, T t ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t, s); }

                public string g2<T>( T t1, T t2 ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t1, t2); }
                public string g2<T>( T t, string s ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t, s); }

                public string g3<T>( T t1, T t2 ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t1, t2); }
                public string g3<T>( string s1, string s2 ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), s1, s2); }

                public string g4<T>( T t, string s ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), t, s); }
                public string g4<T>( string s, T t ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1}, {2})", typeof(T), s, t); }

                // parameter types are recursively monotonically more specific

                public string h1<T>( KeyValuePair<T, T> ktt ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ktt); }
                public string h1<T>( KeyValuePair<string, T> kst ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), kst); }

                public string h2<T>( KeyValuePair<T, T> ktt ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ktt); }
                public string h2<T>( KeyValuePair<T, string> kts ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), kts); }

                public string h3<T>( KeyValuePair<T, T> ktt ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), ktt); }
                public string h3<T>( KeyValuePair<string, string> kss ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), kss); }

                public string h4<T>( KeyValuePair<T, string> kts ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), kts); }
                public string h4<T>( KeyValuePair<string, T> kst ) { return MethodBase.GetCurrentMethod().MethodFormat("<{0}>({1})", typeof(T), kst); }
            }

            // TODO: non-lifted operator is more specific
        }

        [TestMethod]
        public void CallBetterOverloadedMethodsImplicitConversions()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterOverloadedMethods.ImplicitConversions();
                lua["x"] = x;

                var r = lua.Do("return x.f1('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1("x", "y"), r[0]);

                r = lua.Do("return x.f2('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f2("x", "y"), r[0]);

                r = lua.Do("return x.f3('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f3("x", "y"), r[0]);

                try
                {
                    r = lua.Do("return x.f4('x', 'y')");

                    Assert.Fail();
                }
                catch (AmbiguousMatchException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallBetterOverloadedMethodsGeneric()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterOverloadedMethods.Generic();
                lua["x"] = x;

                var r = lua.Do("return x.f1('x')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1("x"), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterOverloadedMethodsParamArray()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterOverloadedMethods.ParamArray();
                lua["x"] = x;

                var r = lua.Do("return x.f1('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1("x", "y"), r[0]);

                r = lua.Do("return x.g1('x', 'y', 'z')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g1("x", "y", "z"), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterOverloadedMethodsDefaultParams()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterOverloadedMethods.DefaultParams();

                lua["x"] = x;

                var r = lua.Do("return x.f1('x')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1("x"), r[0]);

                try
                {
                    r = lua.Do("return x.f2('x')");

                    Assert.Fail();
                }
                catch (AmbiguousMatchException)
                {
                    // expected
                }
            }
        }

        [TestMethod]
        public void CallBetterOverloadedMethodsMoreSpecificParamTypes()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterOverloadedMethods.MoreSpecificParamTypes();
                lua["x"] = x;

                // T is unbound for one of the potential methods for this call
                var r = lua.Do("return x.f1('x')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1("x"), r[0]);

                lua["String"] = typeof(string);

                // T must be explicitly bound to exersize overload resolution for this call
                r = lua.Do("return x.f1{_ = {String}}('x')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1<string>("x"), r[0]);

                var ls = new List<string>();
                lua["ls"] = ls;

                r = lua.Do("return x.f2(ls)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f2(ls), r[0]);

                var las = new List<string[]>();
                lua["las"] = las;

                r = lua.Do("return x.f3(las)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f3(las), r[0]);

                var @as = new string[] { "x" };
                lua["as"] = @as;

                r = lua.Do("return x.f4(as)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f4(@as), r[0]);

                var als = new List<string>[] { new List<string> { "x" } };
                lua["als"] = als;

                r = lua.Do("return x.f5(als)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f5(als), r[0]);

                r = lua.Do("return x.g1('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g1("x", "y"), r[0]);

                r = lua.Do("return x.g2('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g2("x", "y"), r[0]);

                r = lua.Do("return x.g3('x', 'y')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g3("x", "y"), r[0]);

                try
                {
                    r = lua.Do("return x.g4('x', 'y')");

                    Assert.Fail();
                }
                catch (AmbiguousMatchException)
                {
                    // expected
                }

                var kss = new KeyValuePair<string, string>("x", "x");
                lua["kss"] = kss;

                r = lua.Do("return x.h1(kss)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h1(kss), r[0]);

                r = lua.Do("return x.h2(kss)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h2(kss), r[0]);

                r = lua.Do("return x.h3(kss)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h3(kss), r[0]);

                try
                {
                    r = lua.Do("return x.h4(kss)");

                    Assert.Fail();
                }
                catch (AmbiguousMatchException)
                {
                    // expected
                }
            }
        }

        [Serializable]
        private class DerivedList<T> : List<T>
        {
        }

        [Serializable]
        private class DerivedDerivedList<T> : DerivedList<T>
        {
        }

        private static class BetterConversionFromTypeOverloadedMethods
        {
            [Serializable]
            public class IdentityConversion
            {
                // identity conversion is better
                public string f( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                public string f( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            }

            [Serializable]
            public class ConversionTargetSignedUnsigned
            {
                // signed integral type is better than some unsigned
                public string f( uint u ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", u); }
                public string f( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
            }

            // one-way implicit conversion is better
            public static class ConversionTargetImplicitConversion
            {
                // - implicit numeric conversion
                [Serializable]
                public class Numeric
                {
                    public string num_f( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
                    public string num_f( short s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
                }

                // - implicit nullable conversion
                [Serializable]
                public class Nullable
                {
                    public string f1( int? ni ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ni); }
                    public string f1( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

                    public string g1( int? ni ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ni); }
                    public string g1( short? ns ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ns); }
                }

                // - implicit reference conversion
                [Serializable]
                public class Reference
                {
                    // reference-type to object
                    public string f1( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                    public string f1( List<string> ls ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ls); }

                    // class-type to base class-type
                    public string g1( List<string> ls ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ls); }
                    public string g1( DerivedList<string> ls ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ls); }

                    // class-type to implemented interface-type
                    public string h1( IList<string> ils ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ils); }
                    public string h1( List<string> ls ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ls); }

                    // interface-type to base interface-type
                    public string i1( IEnumerable<string> ies ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ies); }
                    public string i1( IList<string> ils ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ils); }

                    // array-type to same-dimensionality array-type ...
                    public string j1( IEnumerable<string>[] aies ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { aies }); }
                    public string j1( IList<string>[] ails ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { ails }); }

                    // single-dimension array-type to IList<> ...
                    public string k1( IList<object> ilo ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ilo); }
                    public string k1( object[] ao ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { ao }); }
                }

                // - implicit boxing conversion
                [Serializable]
                public class Boxing
                {
                    // value-type to object
                    public string f1( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                    public string f1( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

                    // non-nullable-value-type to ValueType
                    public string g1( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                    public string g1( ValueType e ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", e); }

                    // non-nullable-value-type to implemented interface
                    public string h1( IComparable<int> o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                    public string h1( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

                    // nullable-value-type to reference-type where non-nullable-value-type has boxing conversion
                    // not meaningfully testable

                    // enum-type to Enum
                    public string j1( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
                    public string j1( Enum e ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", e); }
                }
            }

            [Serializable]
            public class Misc
            {
                public string f( IEnumerator<string> ies ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ies); }
                public string f( List<string>.Enumerator lse ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", lse); }

                public string g( IEnumerable<object> ieo ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ieo); }
                public string g( IEnumerable<string> ies ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ies); }

                public string h( List<object>.Enumerator loe ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", loe); }
                public string h( IEnumerator<string> ies ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", ies); }
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsIdentityConversion()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.IdentityConversion();
                lua["x"] = x;

                var r = lua.Do("return x.f('x')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f("x"), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsConversionTargetSignedUnsigned()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.ConversionTargetSignedUnsigned();
                lua["x"] = x;

                var r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(1), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsConversionTargetImplicitConversionNumeric()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.ConversionTargetImplicitConversion.Numeric();
                lua["x"] = x;

                var r = lua.Do("return x.num_f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.num_f((byte)1), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsConversionTargetImplicitConversionNullable()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.ConversionTargetImplicitConversion.Nullable();
                lua["x"] = x;

                var r = lua.Do("return x.f1(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1((byte)1), r[0]);

                r = lua.Do("return x.g1(null)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g1(null), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsConversionTargetImplicitConversionReference()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.ConversionTargetImplicitConversion.Reference();
                lua["x"] = x;

                var ls = new List<string>();
                lua["ls"] = ls;

                var r = lua.Do("return x.f1(ls)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1(ls), r[0]);

                var ddls = new DerivedDerivedList<string>();
                lua["ddls"] = ddls;

                r = lua.Do("return x.g1(ddls)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g1(ddls), r[0]);

                r = lua.Do("return x.h1(ddls)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h1(ddls), r[0]);

                r = lua.Do("return x.i1(ls)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.i1(ls), r[0]);

                var als = new[] { ls };
                lua["als"] = als;

                r = lua.Do("return x.j1(als)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.j1(als), r[0]);

                var @as = new string[0];
                lua["as"] = @as;

                r = lua.Do("return x.k1(as)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.k1(@as), r[0]);
            }
        }

        public enum TestEnum
        {
            Element
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsConversionTargetImplicitConversionBoxing()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.ConversionTargetImplicitConversion.Boxing();
                lua["x"] = x;

                var r = lua.Do("return x.f1(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f1((byte)1), r[0]);

                r = lua.Do("return x.g1(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g1((byte)1), r[0]);

                r = lua.Do("return x.h1(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h1((byte)1), r[0]);

                lua["TestEnum"] = new CLRStaticContext(typeof(TestEnum));

                r = lua.Do("return x.j1(TestEnum.Element)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.j1(TestEnum.Element), r[0]);
            }
        }

        [TestMethod]
        public void CallBetterConversionOverloadedMethodsMisc()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new BetterConversionFromTypeOverloadedMethods.Misc();

                lua["x"] = x;

                var l = new List<string>();
                var e = l.GetEnumerator();
                lua["l"] = l;
                lua["e"] = e;

                var r = lua.Do("return x.f(e)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(e), r[0]);

                r = lua.Do("return x.g(l)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.g(l), r[0]);

                r = lua.Do("return x.h(e)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.h(e), r[0]);
            }
        }

        [Serializable]
        private class CovariantRefParameterMethods
        {
            internal readonly Box box = new Box();

            public void f( ref object o ) { o = 0; box.Value = MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public void g( out object o ) { o = 0; box.Value = MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
        }

        [TestMethod]
        public void CallCovariantRefParameterMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new CovariantRefParameterMethods();

                lua["x"] = x;

                var r = lua.Do("return x.f('x')");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));

                var expected = new CovariantRefParameterMethods();
                object o = "x";
                expected.f(ref o);

                Assert.AreEqual(expected.box.Value, x.box.Value);

                r = lua.Do("return x.g('x')");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));

                o = "x";
                expected.g(out o);

                Assert.AreEqual(expected.box.Value, x.box.Value);
            }
        }

        [Serializable]
        private class CovariantRefArrayParameterMethods
        {
            internal readonly Box box = new Box();

            public void f( ref object[] o ) { o[0] = 0; box.Value = MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { o }); }
            public void g( out object[] o ) { o = new Tuple<int, int>[0]; box.Value = MethodBase.GetCurrentMethod().MethodFormat("({0})", new[] { o }); }
        }

        // doesn't exactly match how C# handles by-ref array covariance
        [TestMethod]
        public void CallRefCovariantArrayParameterMethods()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new CovariantRefArrayParameterMethods();

                lua["x"] = x;
                lua["y"] = new string[] { "y" };

                try
                {
                    var r = lua.Do("return x.f(y)");
                    Assert.Fail();
                }
                catch (ArrayTypeMismatchException)
                {
                    // expected
                }

                // if we matched how C# handles by-ref array covariance, calling g(y) would fail with a type mismatch
                {
                    var r = lua.Do("return x.g(y)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(Tuple<int, int>[]));

                    var expected = new CovariantRefArrayParameterMethods();
                    object[] o = new object[] { "y" };
                    expected.g(out o);

                    Assert.AreEqual(expected.box.Value, x.box.Value);
                }
            }
        }

        [Serializable]
        private class TableParameterMethod
        {
            public void f( LuaTable t ) { t["x"] = true; }
        }

        [TestMethod]
        public void CallTableParameterMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new TableParameterMethod();

                lua["x"] = x;

                var r = lua.Do("t = { ['x'] = false }; x.f{}(t); return t");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(true, (r[0] as LuaTable)["x"]);

                try
                {
                    lua.Do("x.f(t)");
                    Assert.Fail();
                }
                catch (BindingHintsException)
                {
                    // expected
                }
            }
        }

        [Serializable]
        private class DelegateParameterMethod
        {
            public int f( Func<int, int> d, int i ) { return d(i); }
        }

        [TestMethod]
        public void CallDelegateParameterMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["x"] = new DelegateParameterMethod();

                lua.Do("function y( x ) return x + 1 end");

                var r = lua.Do("return x.f(y, 0)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual((double)1, r[0]);
            }
        }
    }

    internal static class MethodCallHelpers
    {
        internal static string Join( string separator, IEnumerable values )
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext())
                return String.Empty;
            var builder = new StringBuilder();
            builder.Append(enumerator.Current ?? "null");
            while (enumerator.MoveNext())
                builder.Append(separator).Append(enumerator.Current ?? "null");
            return builder.ToString();
        }

        internal static string MethodFormat( this MethodBase method, string format, params object[] args )
        {
            return new StringBuilder()
                .Append(method.DeclaringType).Append(' ')
                .Append(method).Append(' ')
                .AppendFormat(format, Array.ConvertAll(args, arg =>
                    arg == null ? "null" :
                    arg is IEnumerable && !(arg is String) ? "{" + Join(", ", arg as IEnumerable) + "}" :
                    arg))
                .ToString();
        }
    }
}
