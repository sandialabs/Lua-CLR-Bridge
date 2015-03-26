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
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using Lua;

    /// <summary>
    /// Represents a non-CLI-object Lua userdata.
    /// </summary>
    public class LuaUserData : LuaTableBase
    {
        [SecurityCritical]
        private readonly IntPtr _pointer;

        [SecurityCritical]
        internal LuaUserData( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
            _pointer = LuaWrapper.lua_touserdata(L, index);
        }

        /// <summary>
        /// Gets the pointer of the userdata.
        /// </summary>
        public IntPtr Pointer
        {
            [SecuritySafeCritical] get { return _pointer; }
        }
    }
}
