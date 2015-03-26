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
    public class DelegateTests : SandboxTestsBase
    {
        [TestMethod]
        public void TestLuaFunctionNilDelegate()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function() return nil end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<Func<object>>();

                var result = @delegate.Invoke();

                Assert.AreEqual(null, result);
            }
        }

        [TestMethod]
        public void TestLuaFunctionVoidDelegate()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function() called = true end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<Action>();

                @delegate.Invoke();

                Assert.AreEqual(true, lua["called"] as bool?);
            }
        }

        public delegate int ValueTypeDelegate( int i );

        public delegate void VoidValueTypeDelegate( int i );

        [TestMethod]
        public void TestLuaFunctionValueTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( i ) return i + 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<ValueTypeDelegate>();

                int r = @delegate.Invoke(1);

                Assert.AreEqual(2, r);
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( i ) params = { i } end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidValueTypeDelegate>();

                @delegate.Invoke(1);

                var @params = lua["params"] as LuaTable;
                Assert.AreEqual((double)1, @params[1]);
            }
        }

        public delegate int RefValueTypeDelegate( ref int i );

        public delegate void VoidRefValueTypeDelegate( ref int i );

        [TestMethod]
        public void TestLuaFunctionRefValueTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( i ) return i, i + 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<RefValueTypeDelegate>();

                int i = 1;
                int r = @delegate.Invoke(ref i);

                Assert.AreEqual(2, i);
                Assert.AreEqual(1, r);
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( i ) return i + 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidRefValueTypeDelegate>();

                int i = 1;
                @delegate.Invoke(ref i);

                Assert.AreEqual(2, i);
            }
        }

        public delegate int OutValueTypeDelegate( out int i );

        public delegate void VoidOutValueTypeDelegate( out int i );

        [TestMethod]
        public void TestLuaFunctionOutValueTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1, 2 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<OutValueTypeDelegate>();

                int i;
                int r = @delegate.Invoke(out i);

                Assert.AreEqual(2, i);
                Assert.AreEqual(1, r);
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidOutValueTypeDelegate>();

                int i;
                @delegate.Invoke(out i);

                Assert.AreEqual(1, i);
            }
        }

        private class DelegateParameter
        {
            public readonly int Member;

            public DelegateParameter( int member )
            {
                this.Member = member;
            }
        }

        private delegate DelegateParameter RefTypeDelegate( DelegateParameter i );

        private delegate void VoidRefTypeDelegate( DelegateParameter i );

        [TestMethod]
        public void TestLuaFunctionRefTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( p ) return DP(p.Member + 1) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<RefTypeDelegate>();

                var p = new DelegateParameter(1);

                DelegateParameter r = @delegate.Invoke(p);

                Assert.AreEqual(2, r.Member);
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( p ) params = { p } end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidRefTypeDelegate>();

                var p = new DelegateParameter(1);

                @delegate.Invoke(p);

                var @params = lua["params"] as LuaTable;
                Assert.AreEqual(p, @params[1]);
            }
        }

        private delegate DelegateParameter RefRefTypeDelegate( ref DelegateParameter i );

        private delegate void VoidRefRefTypeDelegate( ref DelegateParameter i );

        [TestMethod]
        public void TestLuaFunctionRefRefTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( p ) return p, DP(p.Member + 1) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<RefRefTypeDelegate>();

                DelegateParameter p = new DelegateParameter(1);
                DelegateParameter r = @delegate.Invoke(ref p);

                Assert.AreEqual(2, p.Member);
                Assert.AreEqual(1, r.Member);
            }

            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( p ) return DP(p.Member + 1) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidRefRefTypeDelegate>();

                DelegateParameter p = new DelegateParameter(1);
                @delegate.Invoke(ref p);

                Assert.AreEqual(2, p.Member);
            }
        }

        private delegate DelegateParameter OutRefTypeDelegate( out DelegateParameter i );

        private delegate void VoidOutRefTypeDelegate( out DelegateParameter i );

        [TestMethod]
        public void TestLuaFunctionOutRefTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( ) return DP(1), DP(2) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<OutRefTypeDelegate>();

                DelegateParameter p;
                DelegateParameter r = @delegate.Invoke(out p);

                Assert.AreEqual(2, p.Member);
                Assert.AreEqual(1, r.Member);
            }

            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( ) return DP(1) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidOutRefTypeDelegate>();

                DelegateParameter p;
                @delegate.Invoke(out p);

                Assert.AreEqual(1, p.Member);
            }
        }

        private delegate int ManyTypeDelegate( int i1, ref int i2, out int i3, DelegateParameter i4, ref DelegateParameter i5, out DelegateParameter i6 );

        private delegate void VoidManyTypeDelegate( int i1, ref int i2, out int i3, DelegateParameter i4, ref DelegateParameter i5, out DelegateParameter i6 );

        [TestMethod]
        public void TestLuaFunctionManyTypeDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( i1, i2, i4, i5 ) return 0, -2, -3, DP(-5), DP(-6) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<ManyTypeDelegate>();

                int p2 = 2;
                int p3;
                DelegateParameter p5 = new DelegateParameter(5);
                DelegateParameter p6;
                int r = @delegate.Invoke(1, ref p2, out p3, new DelegateParameter(4), ref p5, out p6);

                Assert.AreEqual(-2, p2);
                Assert.AreEqual(-3, p3);
                Assert.AreEqual(-5, p5.Member);
                Assert.AreEqual(-6, p6.Member);
                Assert.AreEqual(0, r);
            }

            using (var lua = new LuaBridge())
            {
                lua["DP"] = new CLRStaticContext(typeof(DelegateParameter));

                var function = lua.Do("return function( i1, i2, i4, i5 ) return -2, -3, DP(-5), DP(-6) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidManyTypeDelegate>();

                int p2 = 2;
                int p3;
                DelegateParameter p5 = new DelegateParameter(5);
                DelegateParameter p6;
                @delegate.Invoke(1, ref p2, out p3, new DelegateParameter(4), ref p5, out p6);

                Assert.AreEqual(-2, p2);
                Assert.AreEqual(-3, p3);
                Assert.AreEqual(-5, p5.Member);
                Assert.AreEqual(-6, p6.Member);
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateTooManyReturns()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1, 2, 3 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<OutValueTypeDelegate>();

                int i;
                int r = @delegate.Invoke(out i);

                Assert.AreEqual(2, i);
                Assert.AreEqual(1, r);
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1, 2 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidOutValueTypeDelegate>();

                int i;
                @delegate.Invoke(out i);

                Assert.AreEqual(1, i);
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateTooFewReturns()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<OutValueTypeDelegate>();

                try
                {
                    int i;
                    int r = @delegate.Invoke(out i);
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidOutValueTypeDelegate>();

                try
                {
                    int i;
                    @delegate.Invoke(out i);
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateOutOfRange()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1, 2 / 0 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<OutValueTypeDelegate>();

                try
                {
                    int i;
                    int r = @delegate.Invoke(out i);
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }

            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1 / 0 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<VoidOutValueTypeDelegate>();

                try
                {
                    int i;
                    @delegate.Invoke(out i);
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateError()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) error('bad!') end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<Action>();

                try
                {
                    @delegate.Invoke();
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaRuntimeException));
                }
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateBox()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return 1 end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<Func<Object>>();

                object r = @delegate.Invoke();

                Assert.AreEqual((double)1, r);
            }
        }

        [TestMethod]
        public void TestLuaFunctionDelegateInt64()
        {
            using (var lua = new LuaBridge())
            {
                var function = lua.Do("return function( ) return CLR.Cast.Int64(1) end")[0] as LuaFunction;

                var @delegate = function.ToDelegate<Func<Int64>>();

                object r = @delegate.Invoke();

                Assert.AreEqual((Int64)1, r);
            }
        }

        [TestMethod]
        public void TestNewDelegateBadArguments()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["C"] = new CLRStaticContext(typeof(MethodGroups));

                lua["Fii"] = typeof(Func<int, int>);

                foreach (var t in new Tuple<String, Type>[] {
                    Tuple.Create("nil, nil", typeof(ArgumentException)),
                    Tuple.Create("nil, function( ) end", typeof(ArgumentException)),
                    Tuple.Create("nil, C.f", typeof(ArgumentException)),
                    Tuple.Create("Fii, nil", typeof(ArgumentException)),
                    Tuple.Create("Fii, C.f", typeof(MissingMethodException)),
                })
                {
                    var args = t.Item1;
                    var exType = t.Item2;

                    try
                    {
                        var r = lua.Do("return CLR.NewDelegate(" + args + ")");

                        Assert.Fail();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsInstanceOfType(ex, exType);
                    }
                }
            }
        }

        [TestMethod]
        public void TestNewLuaFunctionDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["D"] = typeof(Func<Object, Object>);

                var r = lua.Do("return CLR.NewDelegate(D, function( o ) return o end)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Func<Object, Object>));
            }
        }

        public class MethodGroups
        {
            public static string f( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string f( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string f( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

            public static string f( ref object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string f( ref string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string f( ref int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

            public static string g( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string g( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string g( int i ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }

            public static string g( out object o ) { o = 1; return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string g( out string s ) { s = "1"; return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string g( out int i ) { i = 1; return MethodBase.GetCurrentMethod().MethodFormat("({0})", i); }
        }

        public delegate TResult FuncRef<T, TResult>( ref T arg );

        public delegate TResult FuncOut<T, TResult>( out T arg );

        [TestMethod]
        public void TestNewMethodGroupDelegate()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["C"] = new CLRStaticContext(typeof(MethodGroups));

                foreach (var t in new Tuple<Type, String, Delegate>[] {
                    Tuple.Create(typeof(Func<Object, String>), "f", (Delegate)new Func<Object, String>(MethodGroups.f)),
                    Tuple.Create(typeof(Func<String, String>), "f", (Delegate)new Func<String, String>(MethodGroups.f)),
                    Tuple.Create(typeof(Func<Int32, String>), "f", (Delegate)new Func<Int32, String>(MethodGroups.f)),

                    Tuple.Create(typeof(FuncRef<Object, String>), "f", (Delegate)new FuncRef<Object, String>(MethodGroups.f)),
                    Tuple.Create(typeof(FuncRef<String, String>), "f", (Delegate)new FuncRef<String, String>(MethodGroups.f)),
                    Tuple.Create(typeof(FuncRef<Int32, String>), "f", (Delegate)new FuncRef<Int32, String>(MethodGroups.f)),

                    Tuple.Create(typeof(FuncOut<Object, String>), "f", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, String>), "f", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<Int32, String>), "f", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, String>), "g", (Delegate)new Func<Object, String>(MethodGroups.g)),
                    Tuple.Create(typeof(Func<String, String>), "g", (Delegate)new Func<String, String>(MethodGroups.g)),
                    Tuple.Create(typeof(Func<Int32, String>), "g", (Delegate)new Func<Int32, String>(MethodGroups.g)),

                    Tuple.Create(typeof(FuncRef<Object, String>), "g", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, String>), "g", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<Int32, String>), "g", (Delegate)null),

                    Tuple.Create(typeof(FuncOut<Object, String>), "g", (Delegate)new FuncOut<Object, String>(MethodGroups.g)),
                    Tuple.Create(typeof(FuncOut<String, String>), "g", (Delegate)new FuncOut<String, String>(MethodGroups.g)),
                    Tuple.Create(typeof(FuncOut<Int32, String>), "g", (Delegate)new FuncOut<Int32, String>(MethodGroups.g)),
                })
                {
                    lua["D"] = t.Item1;

                    try
                    {
                        var r = lua.Do("return CLR.NewDelegate(D, C." + t.Item2 + ")");

                        Assert.AreEqual(1, r.Length);
                        Assert.AreEqual(t.Item3, r[0]);
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNull(t.Item3);
                        Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                    }
                }
            }
        }

        public class RelaxedMethodGroups
        {
            public static object foo( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string fos( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static object fso( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string fss( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }

            public static object goo( ref object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string gos( ref object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static object gso( ref string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string gss( ref string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }

            public static object hoo( out object o ) { o = 1; return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string hos( out object o ) { o = 1; return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static object hso( out string s ) { s = "1"; return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
            public static string hss( out string s ) { s = "1"; return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }

            public static object ioo( object o ) { return null; }
            public static int ioi( object o ) { return 0; }
            public static object iio( int i ) { return null; }
            public static int iii( int i ) { return 0; }
        }

        [TestMethod]
        public void TestNewMethodGroupRelaxedDelegate()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["C"] = new CLRStaticContext(typeof(RelaxedMethodGroups));

                foreach (var t in new Tuple<Type, String, Delegate>[] {
                    Tuple.Create(typeof(Func<Object, Object>), "foo", (Delegate)new Func<Object, Object>(RelaxedMethodGroups.foo)),
                    Tuple.Create(typeof(Func<Object, String>), "foo", (Delegate)null),
                    Tuple.Create(typeof(Func<String, Object>), "foo", (Delegate)new Func<String, Object>(RelaxedMethodGroups.foo)),
                    Tuple.Create(typeof(Func<String, String>), "foo", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, Object>), "fos", (Delegate)new Func<Object, Object>(RelaxedMethodGroups.fos)),
                    Tuple.Create(typeof(Func<Object, String>), "fos", (Delegate)new Func<Object, String>(RelaxedMethodGroups.fos)),
                    Tuple.Create(typeof(Func<String, Object>), "fos", (Delegate)new Func<String, Object>(RelaxedMethodGroups.fos)),
                    Tuple.Create(typeof(Func<String, String>), "fos", (Delegate)new Func<String, String>(RelaxedMethodGroups.fos)),

                    Tuple.Create(typeof(Func<Object, Object>), "fso", (Delegate)null),
                    Tuple.Create(typeof(Func<Object, String>), "fso", (Delegate)null),
                    Tuple.Create(typeof(Func<String, Object>), "fso", (Delegate)new Func<String, Object>(RelaxedMethodGroups.fso)),
                    Tuple.Create(typeof(Func<String, String>), "fso", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, Object>), "fss", (Delegate)null),
                    Tuple.Create(typeof(Func<Object, String>), "fss", (Delegate)null),
                    Tuple.Create(typeof(Func<String, Object>), "fss", (Delegate)new Func<String, Object>(RelaxedMethodGroups.fss)),
                    Tuple.Create(typeof(Func<String, String>), "fss", (Delegate)new Func<String, String>(RelaxedMethodGroups.fss)),

                    Tuple.Create(typeof(FuncRef<Object, Object>), "goo", (Delegate)new FuncRef<Object, Object>(RelaxedMethodGroups.goo)),
                    Tuple.Create(typeof(FuncRef<Object, String>), "goo", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, Object>), "goo", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, String>), "goo", (Delegate)null),

                    Tuple.Create(typeof(FuncRef<Object, Object>), "gos", (Delegate)new FuncRef<Object, Object>(RelaxedMethodGroups.gos)),
                    Tuple.Create(typeof(FuncRef<Object, String>), "gos", (Delegate)new FuncRef<Object, String>(RelaxedMethodGroups.gos)),
                    Tuple.Create(typeof(FuncRef<String, Object>), "gos", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, String>), "gos", (Delegate)null),

                    Tuple.Create(typeof(FuncRef<Object, Object>), "gso", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<Object, String>), "gso", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, Object>), "gso", (Delegate)new FuncRef<String, Object>(RelaxedMethodGroups.gso)),
                    Tuple.Create(typeof(FuncRef<String, String>), "gso", (Delegate)null),

                    Tuple.Create(typeof(FuncRef<Object, Object>), "gss", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<Object, String>), "gss", (Delegate)null),
                    Tuple.Create(typeof(FuncRef<String, Object>), "gss", (Delegate)new FuncRef<String, Object>(RelaxedMethodGroups.gss)),
                    Tuple.Create(typeof(FuncRef<String, String>), "gss", (Delegate)new FuncRef<String, String>(RelaxedMethodGroups.gss)),

                    Tuple.Create(typeof(FuncOut<Object, Object>), "hoo", (Delegate)new FuncOut<Object, Object>(RelaxedMethodGroups.hoo)),
                    Tuple.Create(typeof(FuncOut<Object, String>), "hoo", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, Object>), "hoo", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, String>), "hoo", (Delegate)null),

                    Tuple.Create(typeof(FuncOut<Object, Object>), "hos", (Delegate)new FuncOut<Object, Object>(RelaxedMethodGroups.hos)),
                    Tuple.Create(typeof(FuncOut<Object, String>), "hos", (Delegate)new FuncOut<Object, String>(RelaxedMethodGroups.hos)),
                    Tuple.Create(typeof(FuncOut<String, Object>), "hos", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, String>), "hos", (Delegate)null),

                    Tuple.Create(typeof(FuncOut<Object, Object>), "hso", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<Object, String>), "hso", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, Object>), "hso", (Delegate)new FuncOut<String, Object>(RelaxedMethodGroups.hso)),
                    Tuple.Create(typeof(FuncOut<String, String>), "hso", (Delegate)null),

                    Tuple.Create(typeof(FuncOut<Object, Object>), "hss", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<Object, String>), "hss", (Delegate)null),
                    Tuple.Create(typeof(FuncOut<String, Object>), "hss", (Delegate)new FuncOut<String, Object>(RelaxedMethodGroups.hss)),
                    Tuple.Create(typeof(FuncOut<String, String>), "hss", (Delegate)new FuncOut<String, String>(RelaxedMethodGroups.hss)),

                    Tuple.Create(typeof(Func<Object, Object>), "ioo", (Delegate)new Func<Object, Object>(RelaxedMethodGroups.ioo)),
                    Tuple.Create(typeof(Func<Object, Int32 >), "ioo", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Object>), "ioo", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Int32 >), "ioo", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, Object>), "ioi", (Delegate)null),
                    Tuple.Create(typeof(Func<Object, Int32 >), "ioi", (Delegate)new Func<Object, Int32 >(RelaxedMethodGroups.ioi)),
                    Tuple.Create(typeof(Func<Int32,  Object>), "ioi", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Int32 >), "ioi", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, Object>), "iio", (Delegate)null),
                    Tuple.Create(typeof(Func<Object, Int32 >), "iio", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Object>), "iio", (Delegate)new Func<Int32,  Object>(RelaxedMethodGroups.iio)),
                    Tuple.Create(typeof(Func<Int32,  Int32 >), "iio", (Delegate)null),

                    Tuple.Create(typeof(Func<Object, Object>), "iii", (Delegate)null),
                    Tuple.Create(typeof(Func<Object, Int32 >), "iii", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Object>), "iii", (Delegate)null),
                    Tuple.Create(typeof(Func<Int32,  Int32 >), "iii", (Delegate)new Func<Int32,  Int32 >(RelaxedMethodGroups.iii)),
                })
                {
                    lua["D"] = t.Item1;

                    try
                    {
                        var r = lua.Do("return CLR.NewDelegate(D, C." + t.Item2 + ")");

                        Assert.AreEqual(1, r.Length);
                        Assert.AreEqual(t.Item3, r[0]);
                    }
                    catch (Exception ex)
                    {
                        Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                        Assert.IsNull(t.Item3);
                    }
                }
            }
        }

        [TestMethod]
        public void TestCallLuaFunctionDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua.LoadLib("_G");

                lua["D"] = typeof(Func<Object, Object>);

                lua.Do("d = CLR.NewDelegate(D, function( o ) return type(o) end)");

                var r = lua.Do("return d{}({})");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("table", r[0]);
            }
        }

        [TestMethod]
        public void TestCallMethodGroupDelegate()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["C"] = new CLRStaticContext(typeof(MethodGroups));

                lua["D"] = typeof(Func<Object, Object>);

                lua.Do("d = CLR.NewDelegate(D, C.f)");

                var r = lua.Do("return d(3)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(MethodGroups.f((object)3), r[0]);
            }
        }

        [TestMethod]
        public void TestCallDelegateBadArguments()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["C"] = new CLRStaticContext(typeof(MethodGroups));

                lua["D"] = typeof(Func<int, string>);

                lua.Do("d = CLR.NewDelegate(D, C.f)");

                try
                {
                    var r = lua.Do("return d(nil)");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(MissingMethodException));
                }
            }
        }

        // TODO: delegate from generic method?
    }
}
