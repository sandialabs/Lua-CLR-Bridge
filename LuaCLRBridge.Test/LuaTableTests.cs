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
    using System.Linq;
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LuaTableTests : SandboxTestsBase
    {
        [TestMethod]
        public void GetSetTable()
        {
            using (var lua = CreateLuaBridge())
            {
                Assert.IsNotNull(lua.Environment);
                Assert.IsNull(lua.Environment["b"]);

                lua.Environment["b"] = true;
                object r = lua.Environment["b"];

                Assert.IsInstanceOfType(r, typeof(bool));
                Assert.AreEqual(true, r);
            }
        }

        [TestMethod]
        public void IterateTable()
        {
            using (var lua = CreateLuaBridge())
            {
                Assert.IsNotNull(lua.Environment);
                Assert.IsNull(lua.Environment["b"]);

                lua.Environment["b"] = true;

                object r = null;
                foreach (KeyValuePair<object, object> entry in lua.Environment)
                {
                    if (entry.Key.Equals("b"))
                    {
                        Assert.IsNull(r);
                        r = entry.Value;
                    }
                }

                Assert.IsInstanceOfType(r, typeof(bool));
                Assert.AreEqual(true, r);
            }
        }

        [TestMethod]
        public void IterateTableReset()
        {
            using (var lua = CreateLuaBridge())
            {
                var t = lua.NewTable();

                t[0] = 0;
                t[1] = 1;
                t[2] = 2;

                var e = t.GetEnumerator();

                object first = -1;

                Assert.IsTrue(e.MoveNext());
                first = e.Current;

                Assert.IsTrue(e.MoveNext());

                e.Reset();

                Assert.IsTrue(e.MoveNext());
                Assert.AreEqual(first, e.Current);

                while (e.MoveNext())
                    ;

                e.Reset();

                Assert.IsTrue(e.MoveNext());
                Assert.AreEqual(first, e.Current);

                e.Dispose();
            }
        }

        [TestMethod]
        public void IterateTableIncomplete()
        {
            using (var lua = CreateLuaBridge())
            {
                lua.Environment["b1"] = false;
                lua.Environment["b2"] = true;

                foreach (var entry in lua.Environment)
                    break;
            }
        }

        [TestMethod]
        public void TableToArrayPrimitive()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return { 3, 2, 1 }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Converter<object, int> toInt = ( o ) => { var d = (double)o; if (d % 1 == 0) return (int)d; throw new OverflowException(); };
                Assert.IsTrue(Array.ConvertAll((r[0] as LuaTable).RawToArray(), toInt).SequenceEqual(new int[] { 3, 2, 1 }));
            }
        }

        [TestMethod]
        public void TableToArrayObject()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return { {}, false }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                object[] os = Array.ConvertAll((r[0] as LuaTable).RawToArray(), ( o ) => o);
                Assert.AreEqual(2, os.Length);
                Assert.IsInstanceOfType(os[0], typeof(LuaTable));
                Assert.IsInstanceOfType(os[1], typeof(bool));
            }
        }

        [TestMethod]
        public void NewTable()
        {
            using (var lua = CreateLuaBridge())
            {
                LuaTable t = lua.NewTable();
                t["x"] = 1;

                lua["t"] = t;
                var r = lua.Do("return t.x");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(double));
                Assert.AreEqual(1.0, r[0]);
            }
        }

        [TestMethod]
        public void TableIsEmpty()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return {}");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.IsTrue((r[0] as LuaTable).IsEmpty);

                r = lua.Do("return { 'a', 'b', 'c' }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.IsFalse((r[0] as LuaTable).IsEmpty);

                r = lua.Do("return { ['a'] = 1, ['b'] = 2, ['c'] = 3 }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.IsFalse((r[0] as LuaTable).IsEmpty);

                r = lua.Do("return { ['a'] = 1, 'a', ['b'] = 2, 'b', ['c'] = 3, 'c' }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.IsFalse((r[0] as LuaTable).IsEmpty);
            }
        }

        [TestMethod]
        public void TableCount()
        {
            using (var lua = CreateLuaBridge())
            {
                var r = lua.Do("return {}");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(0, (r[0] as LuaTable).Count);

                r = lua.Do("return { 'a', 'b', 'c' }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(3, (r[0] as LuaTable).Count);

                r = lua.Do("return { ['a'] = 1, ['b'] = 2, ['c'] = 3 }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(3, (r[0] as LuaTable).Count);

                r = lua.Do("return { ['a'] = 1, 'a', ['b'] = 2, 'b', ['c'] = 3, 'c' }");

                Assert.AreEqual(1, r.Length);
                Assert.IsInstanceOfType(r[0], typeof(LuaTable));
                Assert.AreEqual(6, (r[0] as LuaTable).Count);
            }
        }

        [TestMethod]
        public void MetaTable()
        {
            using (var lua = CreateLuaBridge())
            {
                var t = lua.NewTable();
                var mt = lua.NewTable();

                t.Metatable = mt;

                mt["__newindex"] = mt;

                Assert.IsNotNull(t.Metatable);
                Assert.IsNotNull(t.Metatable["__newindex"]);

                t["x"] = 1;

                Assert.IsInstanceOfType(mt["x"], typeof(double));
                Assert.AreEqual(1, (double)mt["x"]);
            }
        }

        [TestMethod]
        public void TableCall()
        {
            using (var lua = CreateLuaBridge())
            {
                var t = lua.NewTable();
                var mt = lua.NewTable();

                t.Metatable = mt;

                mt["__call"] = lua.Do("return function( ) return true end")[0];

                var r = t.Call();

                Assert.AreEqual(1, r.Length);
                Assert.AreEqual(true, r[0]);
            }
        }

        [TestMethod]
        public void TableAlreadyDisposed()
        {
            LuaTable t;

            using (var lua = CreateLuaBridge())
            {
                t = lua.NewTable();
            }

            try
            {
                var l = t.Length;
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ObjectDisposedException));
            }
        }
    }
}
