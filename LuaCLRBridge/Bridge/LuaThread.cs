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
    /// Represents a Lua thread (coroutine).
    /// </summary>
    public class LuaThread : LuaBase
    {
        [SecurityCritical]
        internal LuaThread( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
            // nothing to do
        }

        /// <summary>
        /// Creates a new <see cref="LuaThreadBridge"/> that represents the thread.
        /// </summary>
        /// <param name="clrBridge">The global variable that will be set to the interface used in Lua to
        ///     access the CLR.  If <paramref name="clrBridge"/> is <see cref="String.Empty"/>, no global
        ///     variable is set.  If <paramref name="clrBridge"/> is <c>null</c>, the global variable "CLR"
        ///     is set.</param>
        /// <returns>A bridge that represents the thread.</returns>
        public LuaThreadBridge CreateBridge( string clrBridge = null )
        {
            return new LuaThreadBridge(this, clrBridge);
        }

        /// <summary>
        /// Creates a new Lua thread.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the Lua state that the
        ///     thread will share.</param>
        /// <returns>The Lua thread.</returns>
        [SecurityCritical]
        internal static LuaThread Create( ObjectTranslator objectTranslator )
        {
            using (var lockedMainL = objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                LuaWrapper.lua_newthread(L);
                LuaThread thread = new LuaThread(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1);

                return thread;
            }
        }

        [SecurityCritical]
        internal static LuaThread Get( ObjectTranslator objectTranslator, IntPtr L )
        {
            ObjectTranslator.CheckStack(L, 1);

            LuaWrapper.lua_pushthread(L);
            LuaThread thread = new LuaThread(objectTranslator, L, -1);
            LuaWrapper.lua_pop(L, 1);

            return thread;
        }
    }
}
