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
    using System.Collections.Generic;
    using System.Security;
    using Lua;

    internal partial class ObjectTranslator
    {
        /* When an object reference is pushed into Lua multiple times, it must use the same userdata every
         * time -- otherwise the objects cannot be used as keys in Lua tables. */

        private LuaTable _objectUserDatas;  // weak values

        [SecurityCritical]
        private Dictionary<object, UserDataRef> _objectUserDataRefs = new Dictionary<object, UserDataRef>(new IdentityEqualityComparer<object>());

        [SecurityCritical]
        private void InitializeObjectUserDatas( IntPtr L )
        {
            CheckStack(L, 4);  // table + metatable + key + value

            // create cache table with weak values
            LuaWrapper.lua_newtable(L);
            LuaWrapper.lua_pushvalue(L, -1);
            LuaWrapper.lua_pushstring(L, "__mode", Encoding);
            LuaWrapper.lua_pushstring(L, "v", Encoding);
            LuaWrapper.lua_rawset(L, -3);
            LuaWrapper.lua_setmetatable(L, -2);
            _objectUserDatas = new LuaTable(this, L, -1);
            LuaWrapper.lua_pop(L, 1);
        }

        [SecurityCritical]
        private bool PushObjectUserData( IntPtr L, object obj )
        {
            // If the object is still referenced in Lua, we need to push the same userdata.
            UserDataRef userdataRef;
            if (_objectUserDataRefs.TryGetValue(obj, out userdataRef))
            {
                CheckStack(L, 2);  // objectUserDatas + udata

                _objectUserDatas.Push(L);
                LuaWrapper.lua_rawgeti(L, -1, userdataRef.Index);
                LuaWrapper.lua_remove(L, -2); // objectUserDatas

                /* The userdata will be different if the Lua garbage collector cleaned out the cache table
                   but hasn't called __gc on the items it collected yet, so the reference is stale. */
                if (LuaWrapper.lua_touserdata(L, -1) == userdataRef.UserData)
                    return true;

                LuaWrapper.lua_pop(L, 1);
            }

            return false;
        }

        [SecurityCritical]
        private void StoreObjectUserData( IntPtr L, object obj, IntPtr udata )
        {
            CheckStack(L, 2);  // objectUserDatas + udata

            _objectUserDatas.Push(L);
            LuaWrapper.lua_pushvalue(L, -2); // udata
            int index = LuaWrapper.luaL_ref(L, -2);
            LuaWrapper.lua_pop(L, 1); // objectUserDatas

            _objectUserDataRefs[obj] = new UserDataRef(index, udata);
        }

        [SecurityCritical]
        private void ReleaseObjectUserData( object obj, IntPtr udata )
        {
            UserDataRef userdataRef;
            if (_objectUserDataRefs.TryGetValue(obj, out userdataRef))
            {
                /* The userdata will be different if the Lua garbage collector cleaned out the cache table
                   but didn't call __gc on the items it collected until after the target object was pushed
                   again.  In that case, the reference entry has been overwritten, so don't remove it. */
                if (userdataRef.UserData == udata)
                    _objectUserDataRefs.Remove(obj);
            }
        }

        private struct UserDataRef
        {
            public readonly int Index;
            public readonly IntPtr UserData;

            public UserDataRef( int index, IntPtr userdata )
            {
                this.Index = index;
                this.UserData = userdata;
            }
        }
    }
}
