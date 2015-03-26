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
    public class MemberAccessTests : SandboxTestsBase
    {
        [Serializable]
        private class NonExistentMember
        {
        }

        [TestMethod]
        public void GetSetNonExistentMember()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonExistentMember();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x.y");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'y' is not a member of type '" + typeof(NonExistentMember) + "'", ex.Message);
                }

                try
                {
                    var r = lua.Do("x.y = 2");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'y' is not a member of type '" + typeof(NonExistentMember) + "'", ex.Message);
                }
            }
        }

        #region Constructors

        [Serializable]
        private class PublicImplicitConstructor
        {
        }

        [TestMethod]
        public void CallPublicImplicitConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(PublicImplicitConstructor));

                lua["T"] = T;

                var r = lua.Do("return T()");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(PublicImplicitConstructor));
            }
        }

        [Serializable]
        private class PublicConstructor
        {
            public PublicConstructor( int i )
            {
            }
        }

        [TestMethod]
        public void CallPublicConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(PublicConstructor));

                lua["T"] = T;

                var r = lua.Do("return T(1)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(PublicConstructor));
            }
        }

        [Serializable]
        private class NonPublicConstructor
        {
            internal NonPublicConstructor()
            {
            }

            private NonPublicConstructor( int i )
            {
            }

            protected NonPublicConstructor( string s )
            {
            }
        }

        [TestMethod]
        public void CallNonPublicConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(NonPublicConstructor));

                lua["T"] = T;

                try
                {
                    var r = lua.Do("return T()");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'NonPublicConstructor()' is not a member of type '" + typeof(NonPublicConstructor) + "'", ex.Message);
                }

                try
                {
                    var r = lua.Do("return T(1)");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'NonPublicConstructor(" + typeof(double) + ")' is not a member of type '" + typeof(NonPublicConstructor) + "'", ex.Message);
                }

                try
                {
                    var r = lua.Do("return T('test')");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'NonPublicConstructor(" + typeof(string) + ")' is not a member of type '" + typeof(NonPublicConstructor) + "'", ex.Message);
                }
            }
        }

        [Serializable]
        private class PublicInheritedConstructor : PublicConstructor
        {
            public PublicInheritedConstructor( string s )
                : base(0)
            {
            }
        }

        [TestMethod]
        public void CallInheritedConstructor()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(PublicInheritedConstructor));

                lua["T"] = T;

                try
                {
                    var r = lua.Do("return T(1)");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'PublicInheritedConstructor(" + typeof(double) + ")' is not a member of type '" + typeof(PublicInheritedConstructor) + "'", ex.Message);
                }
            }
        }

        #endregion

        // TODO: Events

        #region Fields

        private class StaticField
        {
            public static double y;
        }

        [TestMethod]
        public void GetSetStaticField()
        {
            using (var lua = new LuaBridge())
            {
                StaticField.y = 1;

                var T = new CLRStaticContext(typeof(StaticField));

                lua["T"] = T;

                var r = lua.Do("return T.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(StaticField.y, r[0]);

                lua.Do("T.y = 2");

                Assert.AreEqual(2.0, StaticField.y);
            }
        }

        [Serializable]
        private class PublicField
        {
            public double y;
        }

        [TestMethod]
        public void GetSetPublicField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicField { y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);

                lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicField;
#endif

                Assert.AreEqual(2.0, x.y);
            }
        }

        [Serializable]
        private class PublicInheritedField : PublicField
        {
        }

        [TestMethod]
        public void GetSetPublicInheritedField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicInheritedField { y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);

                r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicInheritedField;
#endif

                Assert.AreEqual(2.0, x.y);
            }
        }

        [Serializable]
        private class PublicNewFieldHidesPublicField : PublicField
        {
            public new double y;
        }

        [TestMethod]
        public void GetSetPublicNewFieldHidesPublicField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewFieldHidesPublicField { y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);

                r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicNewFieldHidesPublicField;
#endif

                Assert.AreEqual(2.0, x.y);
                Assert.AreEqual(default(double), (x as PublicField).y);
            }
        }

        [Serializable]
        private class PublicNewFieldHidesPublicMethod : PublicMethod
        {
            public new double f;
        }

        [TestMethod]
        public void GetSetPublicNewFieldHidesPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewFieldHidesPublicMethod { f = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.f");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f, r[0]);
                Assert.AreNotEqual(x.f(), r[0]);

                r = lua.Do("x.f = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicNewFieldHidesPublicMethod;
#endif

                Assert.AreEqual(2.0, x.f);
            }
        }

        [Serializable]
        private class NonPublicField
        {
            internal double y = 0;

            private double z;

            protected double a = 0;
        }

        [TestMethod]
        public void GetSetNonPublicField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicField();

                lua["x"] = x;

                foreach (string member in new[] { "y", "z", "a" })
                {
                    try
                    {
                        var r = lua.Do("return x." + member);

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicField) + "'", ex.Message);
                    }

                    try
                    {
                        var r = lua.Do("x." + member + " = 2");

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicField) + "'", ex.Message);
                    }
                }
            }
        }

        [Serializable]
        private class NonPublicNewFieldHidesPublicField : PublicField
        {
            internal new double y;
        }

        [TestMethod]
        public void GetSetNonPublicNewFieldHidesPublicField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicNewFieldHidesPublicField { y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(0.0, r[0]);

                r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as NonPublicNewFieldHidesPublicField;
#endif

                Assert.AreEqual(1.0, x.y);
                Assert.AreEqual(2.0, (x as PublicField).y);
            }
        }

        #endregion

        #region Methods

        private class StaticMethod
        {
            public static string f( bool z ) { return MethodBase.GetCurrentMethod().MethodFormat("({0})", z); }
        }

        [TestMethod]
        public void CallStaticMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var T = new CLRStaticContext(typeof(StaticMethod));

                lua["T"] = T;

                var r = lua.Do("return T.f(true)");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(StaticMethod.f(true), r[0]);
            }
        }

        [Serializable]
        private class PublicMethod
        {
            public string f() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }
        }

        [TestMethod]
        public void CallPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(), r[0]);
            }
        }

        [Serializable]
        private class PublicInheritedMethod : PublicMethod
        {
        }

        [TestMethod]
        public void CallPublicInheritedMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicInheritedMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(), r[0]);
            }
        }

        [Serializable]
        private class PublicNewMethodHidesPublicMethod : PublicMethod
        {
            public new string f() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }
        }

        [TestMethod]
        public void CallPublicNewMethodHidesPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewMethodHidesPublicMethod();

                lua["x"] = x;

                var r = lua.Do("return x.f()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.f(), r[0]);
                Assert.AreNotEqual((x as PublicMethod).f(), r[0]);
            }
        }

        [Serializable]
        private class PublicMethodHidesPublicField : PublicField
        {
            public new string y() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }
        }

        [TestMethod]
        public void CallPublicMethodHidesPublicField()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicMethodHidesPublicField();

                lua["x"] = x;

                var r = lua.Do("return x.y()");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(x.y(), r[0]);
                Assert.AreNotEqual((x as PublicField).y, r[0]);
            }
        }

        [Serializable]
        private class NonPublicMethod
        {
            internal string f() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }

            private string g() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }

            protected string h() { return MethodBase.GetCurrentMethod().MethodFormat("()"); }
        }

        [TestMethod]
        public void CallNonPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicMethod();

                lua["x"] = x;

                foreach (string member in new[] { "f", "g", "h" })
                {
                    try
                    {
                        lua.Do("return x." + member + "()");

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicMethod) + "'", ex.Message);
                    }
                }
            }

        }

        #endregion

        #region Nested Types

        [Serializable]
        private class PublicNestedType
        {
            public class C
            {
            }
        }

        [TestMethod]
        public void GetPublicNestedType()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["T"] = new CLRStaticContext(typeof(PublicNestedType));

                var r = lua.Do("return T.C");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(CLRStaticContext));
                Assert.AreEqual(typeof(PublicNestedType.C), (r[0] as CLRStaticContext).ContextType);
            }
        }

        [TestMethod]
        public void GetPublicNestedTypeFromInstance()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["t"] = new PublicNestedType();

                try
                {
                    lua.Do("return t.C");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'C' is not a member of type '" + typeof(PublicNestedType) + "'", ex.Message);
                }
            }
        }

        [Serializable]
        private class PublicInheritedNestedType : PublicNestedType
        {
        }

        [TestMethod]
        public void GetPublicInheritedNestedType()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["T"] = new CLRStaticContext(typeof(PublicInheritedNestedType));

                try
                {
                    var r = lua.Do("return T.C");  // TODO: maybe this shouldn't fail

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'C' is not a member of type '" + typeof(PublicInheritedNestedType) + "'", ex.Message);
                }
            }
        }

        private class NonPublicNestedType
        {
            internal class C
            {
            }

            private class D
            {
            }

            protected class E
            {
            }
        }

        [TestMethod]
        public void GetNonPublicNestedType()
        {
            using (var lua = CreateLuaBridge())
            {
                lua["T"] = new CLRStaticContext(typeof(NonPublicNestedType));

                foreach (string member in new[] { "C", "D", "E" })
                {
                    try
                    {
                        lua.Do("return T." + member);

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicNestedType) + "'", ex.Message);
                    }
                }
            }
        }

        #endregion

        #region Properties

        private class StaticProperty
        {
            public static double y { get; set; }
        }

        [TestMethod]
        public void GetSetStaticProperty()
        {
            using (var lua = new LuaBridge())
            {
                StaticProperty.y = 1;

                var T = new CLRStaticContext(typeof(StaticProperty));

                lua["T"] = T;

                var r = lua.Do("return T.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(StaticProperty.y, r[0]);

                lua.Do("T.y = 2");

                Assert.AreEqual(2.0, StaticProperty.y);
            }
        }

        [Serializable]
        private class PublicPropertyGetter
        {
            internal double _y;
            public double y { get { return _y; } }
        }

        [TestMethod]
        public void GetPublicProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicPropertyGetter { _y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [Serializable]
        private class PublicPropertySetter
        {
            internal double _y;
            public double y { set { _y = value; } }
        }

        [TestMethod]
        public void SetPublicProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicPropertySetter { _y = 1 };

                lua["x"] = x;

                lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicPropertySetter;
#endif

                Assert.AreEqual(2.0, x._y);
            }
        }


        [Serializable]
        private class PublicInheritedPropertyGetter : PublicPropertyGetter
        {
        }

        [TestMethod]
        public void GetPublicInheritedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicInheritedPropertyGetter { _y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [Serializable]
        private class PublicInheritedPropertySetter : PublicPropertySetter
        {
        }

        [TestMethod]
        public void SetPublicInheritedProperty()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicInheritedPropertySetter { _y = 1 };

                lua["x"] = x;

                lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicInheritedPropertySetter;
#endif

                Assert.AreEqual(2.0, x._y);
            }
        }

        [Serializable]
        private class PublicNewPropertyGetterHidesPublicPropertyGetter : PublicPropertyGetter
        {
            internal new double _y;
            public new double y { get { return _y; } }
        }

        [TestMethod]
        public void GetPublicNewPropertyGetterHidesPublicPropertyGetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewPropertyGetterHidesPublicPropertyGetter { _y = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.y");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [Serializable]
        private class PublicNewPropertySetterHidesPublicPropertySetter : PublicPropertySetter
        {
            internal new double _y;
            public new double y { set { _y = value; } }
        }

        [TestMethod]
        public void SetPublicNewPropertySetterHidesPublicPropertySetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewPropertySetterHidesPublicPropertySetter { _y = 1 };

                lua["x"] = x;

                var r = lua.Do("x.y = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicNewPropertySetterHidesPublicPropertySetter;
#endif

                Assert.AreEqual(2.0, x._y);
                Assert.AreEqual(default(double), (x as PublicPropertySetter)._y);
            }
        }

        [Serializable]
        private class PublicNewPropertyGetterHidesPublicMethod : PublicMethod
        {
            internal double _f;
            public new double f { get { return _f; } }
        }

        [TestMethod]
        public void GetPublicNewPropertyGetterHidesPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewPropertyGetterHidesPublicMethod { _f = 1 };

                lua["x"] = x;

                var r = lua.Do("return x.f");

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [Serializable]
        private class PublicNewPropertySetterHidesPublicMethod : PublicMethod
        {
            internal double _f;
            public new double f { set { _f = value; } }
        }

        [TestMethod]
        public void SetPublicNewPropertySetterHidesPublicMethod()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicNewPropertySetterHidesPublicMethod { _f = 1 };

                lua["x"] = x;

                var r = lua.Do("x.f = 2");

#if !NO_SANDBOX
                // object needs to be copied back from sandbox
                x = lua["x"] as PublicNewPropertySetterHidesPublicMethod;
#endif

                Assert.AreEqual(2.0, x._f);
            }
        }

        [Serializable]
        private class NonPublicPropertyGetter
        {
            internal double _y = 0;
            internal double y { get { return _y; } }

            private double _z = 0;
            private double z { get { return _z; } }

            protected double _a = 0;
            protected double a { get { return _a; } }
        }

        [TestMethod]
        public void GetNonPublicPropertyGetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicPropertyGetter();

                lua["x"] = x;

                foreach (string member in new[] { "y", "z", "a" })
                {
                    try
                    {
                        var r = lua.Do("return x." + member);

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicPropertyGetter) + "'", ex.Message);
                    }
                }
            }
        }

        [Serializable]
        private class PublicPropertyNonPublicGetter
        {
            internal double _y;
            public double y { internal get { return _y; } set { _y = value; } }

            private double _z;
            public double z { private get { return _z; } set { _z = value; } }

            protected double _a;
            public double a { protected get { return _a; } set { _a = value; } }
        }

        [TestMethod]
        public void GetPublicPropertyNonPublicGetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicPropertyNonPublicGetter();

                lua["x"] = x;

                foreach (string member in new[] { "y", "z", "a" })
                {
                    try
                    {
                        var r = lua.Do("return x." + member);

                        Assert.Fail();
                    }
                    catch (MethodAccessException ex)
                    {
                        Assert.AreEqual("'" + typeof(PublicPropertyNonPublicGetter) + "." + member + "' is not get-accessible", ex.Message);
                    }
                }
            }
        }

        [Serializable]
        private class NonPublicPropertySetter
        {
            internal double _y;
            internal double y { set { _y = value; } }

            private double _z;
            private double z { set { _z = value; } }

            protected double _a;
            protected double a { set { _a = value; } }
        }

        [TestMethod]
        public void SetNonPublicPropertySetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicPropertySetter { _y = 1 };

                lua["x"] = x;

                foreach (string member in new[] { "y", "z", "a" })
                {
                    try
                    {
                        var r = lua.Do("x." + member + " = 2");

                        Assert.Fail();
                    }
                    catch (MissingMemberException ex)
                    {
                        Assert.AreEqual("'" + member + "' is not a member of type '" + typeof(NonPublicPropertySetter) + "'", ex.Message);
                    }
                }
            }
        }

        [Serializable]
        private class PublicPropertyNonPublicSetter
        {
            internal double _y;
            public double y { get { return _y; } internal set { _y = value; } }

            private double _z;
            public double z { get { return _z; } private set { _z = value; } }

            protected double _a;
            public double a { get { return _a; } protected set { _a = value; } }
        }

        [TestMethod]
        public void SetPublicPropertyNonPublicSetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new PublicPropertyNonPublicSetter { _y = 1 };

                lua["x"] = x;

                foreach (string member in new[] { "y", "z", "a" })
                {
                    try
                    {
                        var r = lua.Do("x." + member + " = 2");

                        Assert.Fail();
                    }
                    catch (MethodAccessException ex)
                    {
                        Assert.AreEqual("'" + typeof(PublicPropertyNonPublicSetter) + "." + member + "' is not set-accessible", ex.Message);
                    }
                }
            }
        }

        [Serializable]
        private class NonPublicPropertyGetterHidesPublicPropertyGetter : PublicPropertyGetter
        {
            internal new double _y = 0;
            internal new double y { get { return _y; } }
        }

        [TestMethod]
        public void GetNonPublicPropertyGetterHidesPublicPropertyGetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicPropertyGetterHidesPublicPropertyGetter();

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x.y");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'y' is not a member of type '" + typeof(NonPublicPropertyGetterHidesPublicPropertyGetter) + "'", ex.Message);
                }
            }
        }

        [Serializable]
        private class NonPublicPropertySetterHidesPublicPropertySetter : PublicPropertySetter
        {
            internal new double _y;
            internal new double y { set { _y = value; } }
        }

        [TestMethod]
        public void SetNonPublicPropertySetterHidesPublicPropertySetter()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new NonPublicPropertySetterHidesPublicPropertySetter { _y = 1 };

                lua["x"] = x;

                try
                {
                    var r = lua.Do("x.y = 2");

                    Assert.Fail();
                }
                catch (MissingMemberException ex)
                {
                    Assert.AreEqual("'y' is not a member of type '" + typeof(NonPublicPropertySetterHidesPublicPropertySetter) + "'", ex.Message);
                }
            }
        }

        #endregion
    }
}
