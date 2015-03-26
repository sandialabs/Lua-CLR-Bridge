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
    public class EventTests : SandboxTestsBase
    {
        public class Event<T, TResult>
        {
            public event Func<T, TResult> e;

            public TResult f( T i )
            {
                return e == null ? default(TResult) : e(i);
            }
        }

        public class BadArgMethodGroup
        {
            public static string f( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
        }

        [TestMethod]
        public void TestAddRemoveHandlerBadArguments()
        {
            using (var lua = new LuaBridge())
            {
                lua["x"] = new Event<Int32, String>();

                lua["BadArgMethodGroup"] = new CLRStaticContext(typeof(BadArgMethodGroup));

                lua["dis"] = new Func<int, int>(( i ) => 0);

                foreach (var t in new Tuple<String, Type>[] {
                    Tuple.Create("nil, nil", typeof(AmbiguousMatchException)),
                    Tuple.Create("nil, function( ) end", typeof(ArgumentException)),
                    Tuple.Create("x.f, function( ) end", typeof(MissingMemberException)),
                    Tuple.Create("x.e, nil", typeof(AmbiguousMatchException)),
                    Tuple.Create("x.e, BadArgMethodGroup.f", typeof(MissingMethodException)),
                    Tuple.Create("x.e, dis", typeof(InvalidCastException)),
                })
                {
                    var args = t.Item1;
                    var exType = t.Item2;

                    try
                    {
                        var r = lua.Do("return CLR.AddHandler(" + args + ")");

                        Assert.Fail();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsInstanceOfType(ex, exType);
                    }

                    try
                    {
                        var r = lua.Do("return CLR.RemoveHandler(" + args + ")");

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
        public void TestAddRemoveEventHandlerLuaFunction()
        {
            using (var lua = new LuaBridge())
            {
                var x = new Event<Int32, String>();

                lua["x"] = x;

                lua.Do("function f( i ) return '' .. i end");

                lua.Do("CLR.AddHandler(x.e, f)");

                var r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("1", r[0]);

                lua.Do("CLR.RemoveHandler(x.e, f)");

                r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                /* test interoperability */

                lua.Do("CLR.AddHandler(x.e, f)");

                Assert.AreEqual("1", x.f(1));

                x.e -= (lua["f"] as LuaFunction).ToDelegate<Func<int, string>>();

                Assert.IsNull(x.f(1));

                x.e += (lua["f"] as LuaFunction).ToDelegate<Func<int, string>>();

                Assert.AreEqual("1", x.f(1));

                lua.Do("CLR.RemoveHandler(x.e, f)");

                Assert.IsNull(x.f(1));
            }
        }

        [TestMethod]
        public void TestAddRemoveEventHandlerLuaFunctionDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["Func"] = typeof(Func<int, string>);

                var x = new Event<Int32, String>();

                lua["x"] = x;

                lua.Do("function f( i ) return '' .. i end");

                lua.Do("CLR.AddHandler(x.e, CLR.NewDelegate(Func, f))");

                var r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual("1", r[0]);

                lua.Do("CLR.RemoveHandler(x.e, CLR.NewDelegate(Func, f))");

                r = lua.Do("return x.f(1)");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                /* test interoperability */

                lua.Do("CLR.AddHandler(x.e, CLR.NewDelegate(Func, f))");

                Assert.AreEqual("1", x.f(1));

                x.e -= (lua["f"] as LuaFunction).ToDelegate<Func<int, string>>();

                Assert.IsNull(x.f(1));

                x.e += (lua["f"] as LuaFunction).ToDelegate<Func<int, string>>();

                Assert.AreEqual("1", x.f(1));

                lua.Do("CLR.RemoveHandler(x.e, CLR.NewDelegate(Func, f))");

                Assert.IsNull(x.f(1));
            }
        }

        public class MethodGroup
        {
            public static string f( object o ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", o); }
            public static string f( string s ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", s); }
        }

        [TestMethod]
        public void TestAddRemoveEventHandlerMethodGroup()
        {
            using (var lua = new LuaBridge())
            {
                lua["MethodGroup"] = new CLRStaticContext(typeof(MethodGroup));

                var x = new Event<String, String>();

                lua["x"] = x;

                lua.Do("CLR.AddHandler(x.e, MethodGroup.f)");

                var r = lua.Do("return x.f('test')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(MethodGroup.f("test"), r[0]);

                lua.Do("CLR.RemoveHandler(x.e, MethodGroup.f)");

                r = lua.Do("return x.f('test')");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                /* test interoperability */

                lua.Do("CLR.AddHandler(x.e, MethodGroup.f)");

                Assert.AreEqual(MethodGroup.f("test"), x.f("test"));

                x.e -= MethodGroup.f;

                Assert.IsNull(x.f("test"));

                x.e += MethodGroup.f;

                Assert.AreEqual(MethodGroup.f("test"), x.f("test"));

                lua.Do("CLR.RemoveHandler(x.e, MethodGroup.f)");

                Assert.IsNull(x.f("test"));
            }
        }

        [TestMethod]
        public void TestAddRemoveEventHandlerMethodGroupDelegate()
        {
            using (var lua = new LuaBridge())
            {
                lua["MethodGroup"] = new CLRStaticContext(typeof(MethodGroup));

                lua["Func"] = typeof(Func<String, String>);

                var x = new Event<String, String>();

                lua["x"] = x;

                lua.Do("CLR.AddHandler(x.e, CLR.NewDelegate(Func, MethodGroup.f))");

                var r = lua.Do("return x.f('test')");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(MethodGroup.f("test"), r[0]);

                lua.Do("CLR.RemoveHandler(x.e, CLR.NewDelegate(Func, MethodGroup.f))");

                r = lua.Do("return x.f('test')");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                /* test interoperability */

                lua.Do("CLR.AddHandler(x.e, CLR.NewDelegate(Func, MethodGroup.f))");

                Assert.AreEqual(MethodGroup.f("test"), x.f("test"));

                x.e -= MethodGroup.f;

                Assert.IsNull(x.f("test"));

                x.e += MethodGroup.f;

                Assert.AreEqual(MethodGroup.f("test"), x.f("test"));

                lua.Do("CLR.RemoveHandler(x.e, CLR.NewDelegate(Func, MethodGroup.f))");

                Assert.IsNull(x.f("test"));
            }
        }
    }
}
