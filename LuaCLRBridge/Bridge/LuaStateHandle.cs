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
    using System.Runtime.InteropServices;
    using System.Security;
    using Lua;

    [SecurityCritical]
    internal class LuaStateHandle : CriticalHandle
    {
        [SecurityCritical]
        internal LuaStateHandle( IntPtr handle )
            : base(IntPtr.Zero)
        {
            this.handle = handle;
        }

        internal IntPtr Handle
        {
            [SecurityCritical]
            get { return handle; }
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero; }
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            LuaWrapper.lua_close(handle);

            return true;
        }
    }
}
