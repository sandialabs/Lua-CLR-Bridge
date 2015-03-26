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
    using System.Collections.Specialized;
    using System.Security;
    using Lua;

    internal partial class ObjectTranslator
    {
        /* When a delegate is created for a Lua function multiple times, it must be the same delegate every
         * time -- otherwise the delegates cannot be compared for equality, which makes removing them from
         * multicast delegates difficult. */

        private LuaTable _luaFunctionDelegates;  // weak keys

        [SecurityCritical]
        private void InitializeLuaFunctionDelegates( IntPtr L )
        {
            CheckStack(L, 4);  // table + metatable + key + value

            // create cache table with weak keys
            LuaWrapper.lua_newtable(L);
            LuaWrapper.lua_pushvalue(L, -1);
            LuaWrapper.lua_pushstring(L, "__mode", Encoding);
            LuaWrapper.lua_pushstring(L, "k", Encoding);
            LuaWrapper.lua_rawset(L, -3);
            LuaWrapper.lua_setmetatable(L, -2);
            _luaFunctionDelegates = new LuaTable(this, L, -1);
            LuaWrapper.lua_pop(L, 1);
        }

        [SecuritySafeCritical]
        internal bool LookupLuaFunctionDelegate( LuaFunctionBase function, Type delegateType, out Delegate @delegate )
        {
            using (var lockedMainL = LockedMainState)
            {
                var L = lockedMainL._L;

                CheckStack(L, 2);  // luaFunctionDelegates + function

                _luaFunctionDelegates.Push(L);
                function.Push(L);
                LuaWrapper.lua_rawget(L, -2);
                var entries = ToUntranslatedObject(L, -1) as ListDictionary;
                LuaWrapper.lua_pop(L, 2);

                if (entries == null)
                {
                    @delegate = null;
                    return false;
                }

                var delegateWeak = entries[delegateType] as WeakReference;
                @delegate = delegateWeak == null ? null : delegateWeak.Target as Delegate;
                return @delegate != null;
            }
        }

        [SecuritySafeCritical]
        internal void StoreLuaFunctionDelegate( LuaFunctionBase function, Type delegateType, ref Delegate @delegate )
        {
            using (var lockedMainL = LockedMainState)
            {
                var L = lockedMainL._L;

                CheckStack(L, 2);  // luaFunctionDelegates + function

                _luaFunctionDelegates.Push(L);
                function.Push(L);
                LuaWrapper.lua_rawget(L, -2);
                var entries = ToUntranslatedObject(L, -1) as ListDictionary;
                LuaWrapper.lua_pop(L, 2);

                if (entries == null)
                {
                    entries = new ListDictionary();

                    CheckStack(L, 3);  // luaFunctionDelegates + function + entries

                    _luaFunctionDelegates.Push(L);
                    function.Push(L);
                    PushUntranslatedObject(L, entries);
                    LuaWrapper.lua_rawset(L, -3);
                    LuaWrapper.lua_pop(L, 1);
                }

                var delegateWeak = entries[delegateType] as WeakReference;
                var delegateWeakTarget = delegateWeak == null ? null : delegateWeak.Target as Delegate;
                if (delegateWeakTarget != null)
                    @delegate = delegateWeakTarget;
                else
                    entries[delegateType] = new WeakReference(@delegate);
            }
        }
    }
}
