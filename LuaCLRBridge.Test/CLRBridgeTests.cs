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
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CLRBridgeTests : SandboxTestsBase
    {
        [TestMethod]
        public void TestCast()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return CLR.Cast.Char('x')");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual(r[0], (double)"x"[0]);

                foreach (dynamic value in new object[] {
                    Double.Epsilon,
                    Double.MaxValue,
                    Double.MinValue,
                    Double.NaN,
                    Double.NegativeInfinity,
                    Double.PositiveInfinity,
                    Int64.MaxValue,
                    UInt64.MinValue })
                {
                    lua["v"] = value;

                    r = lua.Do("return CLR.Cast.Byte(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Byte)value);

                    r = lua.Do("return CLR.Cast.SByte(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(SByte)value);

                    r = lua.Do("return CLR.Cast.Char(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Char)value);

                    r = lua.Do("return CLR.Cast.Int16(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Int16)value);

                    r = lua.Do("return CLR.Cast.UInt16(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(UInt16)value);

                    r = lua.Do("return CLR.Cast.Int32(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Int32)value);

                    r = lua.Do("return CLR.Cast.UInt32(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(UInt32)value);

                    r = lua.Do("return CLR.Cast.Int64(v)");

                    // 64-bit integers get special treatment
                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(Int64));
                    Assert.AreEqual(r[0], (Int64)value);

                    r = lua.Do("return CLR.Cast.UInt64(v)");

                    // 64-bit integers get special treatment
                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(UInt64));
                    Assert.AreEqual(r[0], (UInt64)value);

                    r = lua.Do("return CLR.Cast.Single(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Single)value);

                    r = lua.Do("return CLR.Cast.Double(v)");

                    Assert.AreEqual(1, r.Length);
                    Assert.IsInstanceOfType(r[0], typeof(double));
                    Assert.AreEqual(r[0], (double)(Double)value);
                }
            }
        }

        [TestMethod]
        public void TestType()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.LoadLib("_G");

                var r = lua.Do("return type(CLR.Type)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(string));
                Assert.AreEqual("table", r[0]);
            }
        }

        public enum TestEnum
        { 
            A,
            B,
        }

        [TestMethod]
        public void TestTypeGet()
        {
            using (var lua = CreateLuaBridge())
            {
                // force assembly to load into sandbox
                lua["x"] = TestEnum.A;

                var r = lua.Do("return CLR.Type['" + typeof(TestEnum).FullName + "']");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Type));
                Assert.AreEqual(typeof(TestEnum), r[0] as Type);

                r = lua.Do("return CLR.Type['TypeThatDoesNotExist']");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                lua["TestEnum"] = new CLRStaticContext(typeof(TestEnum));

                r = lua.Do("return CLR.Type[TestEnum]");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(Type));
                Assert.AreEqual(typeof(TestEnum), r[0] as Type);

                try
                {
                    r = lua.Do("return CLR.Type[nil]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaRuntimeException));
                    Assert.IsTrue(ex.Message.Contains("bad argument #2 to '__index' (string expected, got nil)"));
                }
            }
        }

        [TestMethod]
        public void TestStaticGet()
        {
            using (var lua = CreateLuaBridge())
            {
                // force assembly to load into sandbox
                lua["x"] = TestEnum.A;

                var r = lua.Do("return CLR.Static['" + typeof(TestEnum).FullName + "']");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(CLRStaticContext));
                Assert.AreEqual(typeof(TestEnum), (r[0] as CLRStaticContext).ContextType);

                r = lua.Do("return CLR.Static['TypeThatDoesNotExist']");

                Assert.AreEqual(1, r.Length);
                Assert.IsNull(r[0]);

                lua["TestEnum"] = typeof(TestEnum);

                r = lua.Do("return CLR.Static[TestEnum]");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(CLRStaticContext));
                Assert.AreEqual(typeof(TestEnum), (r[0] as CLRStaticContext).ContextType);

                try
                {
                    r = lua.Do("return CLR.Static[nil]");

                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(LuaRuntimeException));
                    Assert.IsTrue(ex.Message.Contains("bad argument #2 to '__index' (string expected, got nil)"));
                }
            }
        }

        [TestMethod]
        public void TestStaticGetMember()
        {
            using (var lua = CreateLuaBridge())
            {
                // force assembly to load into sandbox
                lua["x"] = TestEnum.A;

                var r = lua.Do("return CLR.Static['" + typeof(TestEnum).FullName + "'].A");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(TestEnum));
                Assert.AreEqual(TestEnum.A, r[0]);
            }
        }

        [TestMethod]
        public void TestItems()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new HashSet<string> { "a", "b", "c" };

                lua["x"] = x;

                var r = lua.Do("local r = {}; for v in CLR.Items(x) do r[v] = v end; return r");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Count, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(string));

                    Assert.IsTrue(x.Contains(entry.Key as string));
                }
            }
        }

        [TestMethod]
        public void TestIPairs()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new string[] { "a", "b", "c" };

                lua["x"] = x;

                var r = lua.Do("local r = {}; for k, v in CLR.IPairs(x) do r[k] = v end; return r");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Length, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(double));
                    Assert.IsInstanceOfType(entry.Value, typeof(string));

                    Assert.AreEqual(x[(int)(double)entry.Key], entry.Value as string);
                }
            }
        }

        [TestMethod]
        public void TestPairs()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } };

                lua["x"] = x;

                var r = lua.Do("local r = {}; for k, v in CLR.Pairs(x) do r[k] = v end; return r");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Count, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(string));
                    Assert.IsInstanceOfType(entry.Value, typeof(double));

                    Assert.AreEqual(x[entry.Key as string], (double)entry.Value);
                }
            }
        }

        [TestMethod]
        public void TestDictionaryToTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } };

                lua["x"] = x;

                var r = lua.Do("return CLR.ToTable(x)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Count, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(string));
                    Assert.IsInstanceOfType(entry.Value, typeof(double));

                    Assert.AreEqual(x[entry.Key as string], (double)entry.Value);
                }
            }
        }

        [TestMethod]
        public void TestListToTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new List<string> { "a", "b", "c" };

                lua["x"] = x;

                var r = lua.Do("return CLR.ToTable(x)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Count, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(double));
                    Assert.IsInstanceOfType(entry.Value, typeof(string));

                    Assert.AreEqual(x[(int)(double)entry.Key - 1], entry.Value as string);
                }
            }
        }

        [TestMethod]
        public void TestArrayToTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new string[] { "a", "b", "c" };

                lua["x"] = x;

                var r = lua.Do("return CLR.ToTable(x)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Length, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(double));
                    Assert.IsInstanceOfType(entry.Value, typeof(string));

                    Assert.AreEqual(x[(int)(double)entry.Key - 1], entry.Value as string);
                }
            }
        }

        [TestMethod]
        public void TestSetToTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var x = new HashSet<string> { "a", "b", "c" };

                lua["x"] = x;

                var r = lua.Do("return CLR.ToTable(x)");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(x.Count, (r[0] as LuaTable).Count);

                foreach (var entry in r[0] as LuaTable)
                {
                    Assert.IsInstanceOfType(entry.Key, typeof(string));

                    Assert.IsTrue(x.Contains(entry.Key as string));
                }
            }
        }
    }
}
