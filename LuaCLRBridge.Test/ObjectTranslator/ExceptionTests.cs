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
    public class ExceptionTests : SandboxTestsBase
    {
        [TestMethod]
        public void TestPcallCatchException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["f"] = new Action(() =>
                {
                    throw new Exception("abc");
                });

                var r = lua.Do("return pcall(f.Invoke)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(Exception));
                Assert.AreEqual("abc", ((Exception)r[1]).Message);
            }
        }

        [Serializable]
        private class ExceptionThrower
        {
            public ExceptionThrower()
            {
            }

            public ExceptionThrower( int x )
            {
                throw new InvalidOperationException("ExceptionThrower");
            }

            public int Property
            {
                get { throw new InvalidOperationException("get_Property"); }
                set { throw new InvalidOperationException("set_Property"); }
            }

            public int this[int i]
            {
                get { throw new InvalidOperationException("get_Item"); }
                set { throw new InvalidOperationException("set_Item"); }
            }

            public static ExceptionThrower operator -( ExceptionThrower x )
            {
                throw new InvalidOperationException("op_UnaryNegation");
            }

            public static ExceptionThrower operator +( ExceptionThrower x, ExceptionThrower y )
            {
                throw new InvalidOperationException("op_Addition");
            }

            public static ExceptionThrower operator -( ExceptionThrower x, ExceptionThrower y )
            {
                throw new InvalidOperationException("op_Subtraction");
            }

            public static ExceptionThrower operator *( ExceptionThrower x, ExceptionThrower y )
            {
                throw new InvalidOperationException("op_Multiply");
            }

            public static ExceptionThrower operator /( ExceptionThrower x, ExceptionThrower y )
            {
                throw new InvalidOperationException("op_Divide");
            }

            public static ExceptionThrower operator %( ExceptionThrower x, ExceptionThrower y )
            {
                throw new InvalidOperationException("op_Modulus");
            }

            public static bool operator ==( ExceptionThrower x, object y )
            {
                throw new InvalidOperationException("op_Equality");
            }

            public static bool operator !=( ExceptionThrower x, object y )
            {
                return false;
            }

            public static bool operator <( ExceptionThrower x, int y )
            {
                throw new InvalidOperationException("op_LessThan");
            }

            public static bool operator >( ExceptionThrower x, int y )
            {
                throw new InvalidOperationException("op_GreaterThan");
            }

            public static bool operator <=( ExceptionThrower x, int y )
            {
                throw new InvalidOperationException("op_LessEqual");
            }

            public static bool operator >=( ExceptionThrower x, int y )
            {
                throw new InvalidOperationException("op_GreaterEqual");
            }

            public void Method()
            {
                throw new InvalidOperationException("Method");
            }

            public override string ToString()
            {
                throw new InvalidOperationException("ToString");
            }
        }

        [TestMethod]
        public void TestConstructorThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new CLRStaticContext(typeof(ExceptionThrower));

                var r = lua.Do("return pcall(function() return x(0) end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("ExceptionThrower", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestGetPropertyThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x.Property end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("get_Property", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestSetPropertyThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() x.Property = 1 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("set_Property", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestGetIndexedPropertyThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x.Item[0] end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("get_Item", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestSetIndexedPropertyThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() x.Item[0] = 1 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("set_Item", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestUnaryNegationThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return -x end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_UnaryNegation", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestAdditionThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x + y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Addition", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestSubtractionThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x - y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Subtraction", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestMultiplyThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x * y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Multiply", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestDivideThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x / y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Divide", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestModulusThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x % y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Modulus", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestEqualityThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new Object();

                var r = lua.Do("return pcall(function() return x == y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Equality", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestInequalityThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();
                lua["y"] = new Object();

                var r = lua.Do("return pcall(function() return x ~= y end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_Equality", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestLessThanThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x < 0 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_LessThan", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestGreaterThanThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x > 0 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_GreaterThan", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestLessEqualThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x <= 0 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_LessEqual", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestGreaterEqualThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x >= 0 end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("op_GreaterEqual", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestMethodThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return x.Method() end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("Method", ((InvalidOperationException)r[1]).Message);
            }
        }

        [TestMethod]
        public void TestToStringThrowsException()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");  // for pcall, tostring

                lua["x"] = new ExceptionThrower();

                var r = lua.Do("return pcall(function() return tostring(x) end)");

                Assert.AreEqual(2, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(bool));
                Assert.AreEqual(false, r[0]);
                Assert.IsInstanceOfType(r[1], typeof(InvalidOperationException));
                Assert.AreEqual("ToString", ((InvalidOperationException)r[1]).Message);
            }
        }
    }
}
