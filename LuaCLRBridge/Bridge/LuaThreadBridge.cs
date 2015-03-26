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
    using System.Diagnostics;
    using System.Security;
    using Lua;

    /// <summary>
    /// Represents a coroutine thread of a Lua state.
    /// </summary>
    public class LuaThreadBridge : LuaBridgeBase
    {
        private bool _disposed = false;

        private LuaThread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaThreadBridge"/> class with a new Lua state.
        /// </summary>
        /// <param name="thread">The thread that the bridge will represent.</param>
        /// <param name="clrBridge">The global variable that will be set to the interface used in Lua to
        ///     access the CLR.  If <paramref name="clrBridge"/> is <see cref="String.Empty"/>, no global
        ///     variable is set.  If <paramref name="clrBridge"/> is <c>null</c>, the global variable "CLR"
        ///     is set.</param>
        [SecuritySafeCritical]
        public LuaThreadBridge( LuaThread thread, string clrBridge = null )
            : this(thread, GetL(thread), clrBridge)
        {
            // nothing to do
        }

        [SecurityCritical]
        internal LuaThreadBridge( LuaThread thread, IntPtr threadL, string clrBridge )
            : base(threadL, thread._objectTranslator, clrBridge)
        {
            ObjectTranslator.CheckStack(threadL, 1);

            /* Obtain a separate reference to the thread to ensure that it survives even if
               the original reference is disposed. */
            PushObject(threadL, thread);
            this.thread = PopObject(threadL) as LuaThread;

            Debug.Assert(this.thread != null && !this.thread.IsSameReference(thread), "Thread bridge must have its own reference to thread.");
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="LuaThreadBridge"/> and optionally
        /// releases the managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        [SecuritySafeCritical]
        protected override void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            AssertEmptyStack();

            _disposed = true;

            base.Dispose(disposeManaged);
        }

        [SecurityCritical]
        private static IntPtr GetL( LuaThread thread )
        {
            using (var lockedMainL = thread._objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                thread.Push(L); // thread
                var threadL = LuaWrapper.lua_tothread(L, -1);
                LuaWrapper.lua_pop(L, 1); // thread

                return threadL;
            }
        }
    }
}
