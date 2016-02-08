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
        /// <summary>
        /// The Lua reference table that stores the userdatas that represent CLI objects.
        /// </summary>
        /// <remarks>
        /// This table has weakly referenced values.
        /// 
        /// For as long as a CLI object is referenced by the Lua state, it must always be represented by the
        /// same userdata.  However, if a CLI object handle is pushed into the Lua state, is garbage 
        /// collected because the Lua state no longer references it, and later is pushed into the Lua state
        /// again then the CLI object may be represented by a different userdata than before.
        /// 
        /// If a userdata is garbage collected from this table then the CLI object that it represents was
        /// not otherwise referenced within the Lua state and the CLI object will be represented by a new
        /// userdata if its handle is pushed into the Lua state again.
        /// </remarks>
        private LuaTable _objectUserDatas;  // weak values

        /// <summary>
        /// The mapping from CLI objects that are referenced in the Lua state to copies of the appropriate
        /// entries in <see cref="_objectUserDatas"/>.
        /// </summary>
        /// <remarks>
        /// When a CLI object reference is pushed into Lua multiple times, it must use the same userdata
        /// every time, otherwise CLI objects cannot be used as keys in Lua tables.
        /// </remarks>
        [SecurityCritical]
        private Dictionary<object, UserDataRef> _objectUserDataRefs = new Dictionary<object, UserDataRef>(new IdentityEqualityComparer<object>());

        [SecurityCritical]
        private void InitializeObjectUserDatas( IntPtr L )
        {
            CheckStack(L, 4);  // table + metatable + key + value

            // create table with weak values
            LuaWrapper.lua_newtable(L);
            LuaWrapper.lua_pushvalue(L, -1);
            LuaWrapper.lua_pushstring(L, "__mode", Encoding);
            LuaWrapper.lua_pushstring(L, "v", Encoding);
            LuaWrapper.lua_rawset(L, -3);
            LuaWrapper.lua_setmetatable(L, -2);
            _objectUserDatas = new LuaTable(this, L, -1);
            LuaWrapper.lua_pop(L, 1);
        }

        /// <summary>
        /// Push the userdata that represents a CLI object onto the stack of a Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="obj">The CLI object.</param>
        /// <returns><c>true</c> if the userdata that represents the CLI object was pushed.</returns>
        /// <returns><c>false</c> if the CLI object does not have a userdata that represents it.</returns>
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

                /* The userdata retrieved from the reference table will be different from the expected userdata if
                 * the Lua garbage collector has cleaned out the reference table but hasn't called __gc on the
                 * userdata that it collected yet.  In that case, the userdata cannot be used. */

                /* If the userdata retrieved from the reference table is the expected userdata then it is the
                 * userdata that represents the CLI object. */
                if (LuaWrapper.lua_touserdata(L, -1) == userdataRef.UserData)
                    return true;

                LuaWrapper.lua_pop(L, 1);
            }

            return false;
        }

        /// <summary>
        /// Add a mapping from a CLI object to the userdata that represents it.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="obj">The CLI object.</param>
        /// <param name="udata">The userdata that represents the CLI object.</param>
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

        /// <summary>
        /// Remove the mapping from a CLI object to the userdata that represents it.
        /// </summary>
        /// <param name="obj">The CLI object.</param>
        /// <param name="udata">The userdata the represents the CLI object.</param>
        /// <remarks>
        /// This method is called during Lua garbage collection of a userdata that represents a
        /// CLI object.
        /// </remarks>
        [SecurityCritical]
        private void ReleaseObjectUserData( object obj, IntPtr udata )
        {
            UserDataRef userdataRef;
            if (_objectUserDataRefs.TryGetValue(obj, out userdataRef))
            {
                /* The userdata being released will be different from the expected userdata if the Lua garbage
                 * collector has cleaned out the reference table but didn't call __gc on the userdata it collected
                 * until after the CLI object was pushed again.  In that case, the mapping for the CLI object has
                 * been overwritten with a mapping to a new userdata that represents the CLI object, which must not
                 * be removed. */

                /* If the userdata being released is the expected userdata then the CLI object is not referenced in
                 * the Lua state. */
                if (userdataRef.UserData == udata)
                    _objectUserDataRefs.Remove(obj);
            }
        }

        /// <summary>
        /// A copy of an entry in  <see cref="_objectUserDatas"/>.
        /// </summary>
        private struct UserDataRef
        {
            /// <summary>
            /// The key of the entry.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The value of the entry.
            /// </summary>
            public readonly IntPtr UserData;

            public UserDataRef( int index, IntPtr userdata )
            {
                this.Index = index;
                this.UserData = userdata;
            }
        }
    }
}
