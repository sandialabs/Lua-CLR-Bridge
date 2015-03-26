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
    using System.Security;
    using Lua;

    /// <summary>
    /// Represents a Lua object that may have a metatable.
    /// </summary>
    public abstract class LuaTableBase : LuaFunctionBase
    {
        [SecurityCritical]
        internal LuaTableBase( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
        }

        /// <summary>
        /// Gets the length as reported by the '#' operator in Lua.
        /// </summary>
        public object Length
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 2);  // self + len

                    Push(L); // self

                    LuaWrapper.lua_len(L, -1);
                    object result = _objectTranslator.PopObject(L);

                    LuaWrapper.lua_pop(L, 1); // self

                    return result;
                }
            }
        }

        /// <summary>
        /// Gets the length as reported by the 'rawlen' function in Lua.
        /// </summary>
        [CLSCompliant(false)]
        public UIntPtr RawLength
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 1);

                    Push(L); // self

                    var length = LuaWrapper.lua_rawlen(L, -1);

                    LuaWrapper.lua_pop(L, 1); // self

                    return length;
                }
            }
        }

        /// <summary>
        /// Gets or sets the metatable of the Lua object.
        /// </summary>
        public LuaTable Metatable
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 1);

                    Push(L); // self

                    LuaTable result = LuaWrapper.lua_getmetatable(L, -1) ?
                        _objectTranslator.PopObject(L) as LuaTable :
                        null;

                    LuaWrapper.lua_pop(L, 1); // self

                    return result;
                }
            }

            [SecuritySafeCritical]
            set
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 2);  // self + metatable

                    Push(L); // self
                    value.Push(L);

                    LuaWrapper.lua_setmetatable(L, -2);

                    LuaWrapper.lua_pop(L, 1);
                }
            }
        }

        /// <summary>
        /// Gets and sets elements like an indexing operation in Lua.
        /// </summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The element at the specified index if it exists; otherwise, null.</returns>
        public object this[ object index ]
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 2);  // self + index

                    Push(L); // self
                    _objectTranslator.PushObject(L, index);

                    LuaWrapper.lua_gettable(L, -2);

                    object result = _objectTranslator.PopObject(L);
                    LuaWrapper.lua_pop(L, 1); // self

                    return result;
                }
            }

            [SecuritySafeCritical]
            set
            {
                using (var lockedMainL = _objectTranslator.LockedMainState)
                {
                    var L = lockedMainL._L;

                    ObjectTranslator.CheckStack(L, 3);  // self + index + value

                    Push(L); // self
                    _objectTranslator.PushObject(L, index);
                    _objectTranslator.PushObject(L, value);

                    LuaWrapper.lua_settable(L, -3);

                    LuaWrapper.lua_pop(L, 1); // self
                }
            }
        }
    }
}
