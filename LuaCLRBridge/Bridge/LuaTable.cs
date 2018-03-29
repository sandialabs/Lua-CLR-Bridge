/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using Lua;

    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Unreasonable.")]
    public class LuaTable : LuaTableBase, IEnumerable<KeyValuePair<object, object>>
    {
        [SecurityCritical]
        internal LuaTable( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the table is empty.
        /// </summary>
        public bool IsEmpty
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 3);  // self + key + value

                    Push(L); // self

                    LuaWrapper.lua_pushnil(L);
                    if (LuaWrapper.lua_next(L, -2) != 0)
                    {
                        LuaWrapper.lua_pop(L, 3); // self, key, value

                        return false;
                    }
                    else
                    {
                        LuaWrapper.lua_pop(L, 1); // self

                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of pairs contained in the table.
        /// </summary>
        /// <remarks>
        /// This operation is O(n).
        /// </remarks>
        public int Count
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    int count = 0;

                    ObjectTranslator.CheckStack(L, 3);  // self + key + value

                    Push(L); // self

                    LuaWrapper.lua_pushnil(L);
                    while (LuaWrapper.lua_next(L, -2) != 0)
                    {
                        LuaWrapper.lua_pop(L, 1); // value

                        ++count;
                    }

                    LuaWrapper.lua_pop(L, 1); // table

                    return count;
                }
            }
        }

        /// <summary>
        /// Creates a new Lua table.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the Lua state that the
        ///     table will exist within.</param>
        /// <returns>The Lua table.</returns>
        [SecurityCritical]
        internal static LuaTable Create( ObjectTranslator objectTranslator )
        {
            using (var lockedMainL = objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                LuaWrapper.lua_newtable(L);
                LuaTable table = new LuaTable(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1);

                return table;
            }
        }

        /// <summary>
        /// Creates a new Lua table.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the Lua state that the
        ///     table will exist within.</param>
        /// <param name="arrayCountHint">The number of array entries to allocate.</param>
        /// <param name="recordCountHint">The number of record entries to allocate.</param>
        /// <returns>The Lua table.</returns>
        [SecurityCritical]
        internal static LuaTable Create( ObjectTranslator objectTranslator, int arrayCountHint, int recordCountHint )
        {
            using (var lockedMainL = objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                LuaWrapper.lua_createtable(L, arrayCountHint, recordCountHint);
                LuaTable table = new LuaTable(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1);

                return table;
            }
        }

        /// <summary>
        /// Returns an enumerator that works like the 'next' function in Lua.
        /// </summary>
        /// <returns>An enumerator for the table.</returns>
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Enumerates a Lua table.
        /// </summary>
        internal sealed class Enumerator : MarshalByRefObject, IEnumerator<KeyValuePair<object, object>>
        {
            private LuaThreadBridge _threadBridge;

            private bool _valid = true;

            private KeyValuePair<object, object> _current;

            [SecuritySafeCritical]
            public Enumerator( LuaTable table )
            {
                using (var thread = LuaThread.Create(table._objectTranslator))
                    _threadBridge = thread.CreateBridge();

                Debug.Assert(LuaWrapper.LUA_MINSTACK >= 3, "Insufficient stack.");  // table + key + value

                using (var lockedL = _threadBridge.LockedState)
                {
                    var L = lockedL._L;

                    table.Push(L);

                    LuaWrapper.lua_pushnil(L);
                }
            }

            /// <summary>
            /// Gets the element in the Lua table at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<object, object> Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            /// <summary>
            /// Releases all the resources used by the <see cref="LuaTable.Enumerator"/>.
            /// </summary>
            [SecuritySafeCritical]
            public void Dispose()
            {
                if (_threadBridge != null)
                {
                    using (var lockedL = _threadBridge.LockedState)
                    {
                        var L = lockedL._L;

                        LuaWrapper.lua_pop(L, 2);
                    }

                    _threadBridge.Dispose();
                }

                _threadBridge = null;

                _current = default(KeyValuePair<object, object>);
            }

            /// <summary>
            /// Advances the enumerator to the next element in the Lua table.
            /// </summary>
            /// <returns><c>true</c> if the enumerator successfully advanced to the next element; <c>false</c>
            ///     if the enumerator has passed the end of the Lua table.</returns>
            /// <exception cref="ObjectDisposedException">The <see cref="LuaTable"/> has been disposed.
            ///     </exception>
            [SecuritySafeCritical]
            public bool MoveNext()
            {
                if (_threadBridge == null)
                    throw new ObjectDisposedException(GetType().FullName);

                if (!_valid)
                    return false;

                using (var lockedL = _threadBridge.LockedState)
                {
                    var L = lockedL._L;
                    var objectTranslator = lockedL._objectTranslator;

                    /* stack checked in constructor */

                    if (LuaWrapper.lua_next(L, -2) == 0)
                    {
                        LuaWrapper.lua_pushnil(L);

                        _valid = false;
                        _current = default(KeyValuePair<object, object>);
                    }
                    else
                    {
                        object value = objectTranslator.PopObject(L);
                        object key = objectTranslator.ToObject(L);

                        _current = new KeyValuePair<object, object>(key, value);
                    }

                    return _valid;
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the Lua table.
            /// </summary>
            [SecuritySafeCritical]
            public void Reset()
            {
                using (var lockedL = _threadBridge.LockedState)
                {
                    var L = lockedL._L;

                    LuaWrapper.lua_pop(L, 1);
                    LuaWrapper.lua_pushnil(L);

                    _valid = true;
                    _current = default(KeyValuePair<object, object>);
                }
            }
        }

        /// <summary>
        /// Attempts to convert the elements in the array-portion of the table to an array using raw access
        /// (i.e. ignores metatable).
        /// </summary>
        /// <returns>The array of table elements.</returns>
        [SecuritySafeCritical]
        public object[] RawToArray()
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 2);  // self + value

                Push(L); // self

                var length = (long)LuaWrapper.lua_rawlen(L, -1);

                object[] array = new object[length];

                for (int i = 0; i < length; ++i)
                {
                    LuaWrapper.lua_rawgeti(L, -1, i + 1);

                    var element = _objectTranslator.PopObject(L);

                    try
                    {
                        array[i] = element;
                    }
                    catch
                    {
                        LuaWrapper.lua_pop(L, 1);  // self

                        throw;
                    }
                }

                LuaWrapper.lua_pop(L, 1); // table

                return array;
            }
        }
    }
}
