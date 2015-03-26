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
    public class ArrayTests : SandboxTestsBase
    {
        [TestMethod]
        public void GetArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[] { 0, 1, 2 };

                lua["x"] = x;

                var r = lua.Do("return x[1]");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [TestMethod]
        public void GetArrayElementOutOfRange()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[] { 0, 1, 2 };

                lua["x"] = x;

                try
                {
                    lua.Do("return x[3]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(IndexOutOfRangeException));
                }

                try
                {
                    lua.Do("return x[1.5]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }
            }
        }

        [TestMethod]
        public void GetMultiDimensionalArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[,] { { 0, 1, 2 }, { 3, 4, 5 } };

                lua["x"] = x;

                var r = lua.Do("return x[{1, 1}]");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual(4.0, r[0]);
            }
        }

        [TestMethod]
        public void GetMultiDimensionalArrayElementOutOfRange()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[,] { { 0, 1, 2 }, { 3, 4, 5 } };

                lua["x"] = x;

                try
                {
                    var r = lua.Do("return x[{1, 3}]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(IndexOutOfRangeException));
                }

                try
                {
                    var r = lua.Do("return x[{1, 1.5}]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                try
                {
                    var r = lua.Do("return x[{1, {}}]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }
            }
        }

        [TestMethod]
        public void SetArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[] { 9, 8, 7 };

                lua["x"] = x;

                var r = lua.Do("x[1] = 1");

#if !NO_SANDBOX
                // array needs to be copied back from sandbox
                x = lua["x"] as int[];
#endif

                Assert.AreEqual(1.0, x[1]);
            }
        }

        [TestMethod]
        public void SetArrayElementOutOfRange()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[] { 0, 1, 2 };

                lua["x"] = x;

                try
                {
                    lua.Do("x[3] = 4");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(IndexOutOfRangeException));
                }

                try
                {
                    lua.Do("x[1.5] = 4");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }
            }
        }

        [TestMethod]
        public void SetMultiDimensionalArrayElement()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[,] { { 9, 8, 7 }, { 6, 5, 4 } };

                lua["x"] = x;

                var r = lua.Do("x[{1, 1}] = 1");

#if !NO_SANDBOX
                // array needs to be copied back from sandbox
                x = lua["x"] as int[,];
#endif

                Assert.AreEqual(1.0, x[1, 1]);
            }
        }

        [TestMethod]
        public void SetMultiDimensionalArrayElementOutOfRange()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[,] { { 9, 8, 7 }, { 6, 5, 4 } };

                lua["x"] = x;

                try
                {
                    var r = lua.Do("x[{1, 3}] = 1");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(IndexOutOfRangeException));
                }

                try
                {
                    var r = lua.Do("x[{1, 1.5}] = 1");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                try
                {
                    var r = lua.Do("x[{1, {}}] = 1");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }
            }
        }

        [TestMethod]
        public void SetArrayElementTypeMismatch()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new int[] { };

                lua["x"] = x;

                try
                {
                    var r = lua.Do("x[1] = nil");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }

                try
                {
                    var r = lua.Do("x[1] = {}");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
                }
            }
        }
    }
}
