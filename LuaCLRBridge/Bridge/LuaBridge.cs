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
    using System.Text;
    using Lua;

    /// <summary>
    /// Represents the main thread of a Lua state.
    /// </summary>
    public class LuaBridge : LuaBridgeBase
    {
        [SecurityCritical]
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaBridge"/> class with a new Lua state.
        /// </summary>
        /// <param name="clrBridge">The global variable that will be set to the interface used in Lua to
        ///     access the CLR.  If <paramref name="clrBridge"/> is <see cref="String.Empty"/>, no global
        ///     variable is set.  If <paramref name="clrBridge"/> is <c>null</c>, the global variable "CLR"
        ///     is set.</param>
        /// <param name="encoding">The character encoding to use when translating between
        ///     <see cref="String"/> and strings in Lua.  If <paramref name="encoding"/> is <c>null</c>,
        ///     iso-8859-1 is used.</param>
        [SecuritySafeCritical]
        public LuaBridge( string clrBridge = null, Encoding encoding = null )
            : base(LuaWrapper.luaL_newstate(), clrBridge, encoding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaBridge"/> class with a specified Lua state.
        /// </summary>
        /// <param name="L">The unmanaged Lua state.</param>
        /// <param name="clrBridge">The global variable that will be set to the interface used in Lua to
        ///     access the CLR.  If <paramref name="clrBridge"/> is <see cref="String.Empty"/>, no global
        ///     variable is set.  If <paramref name="clrBridge"/> is <c>null</c>, the global variable "CLR"
        ///     is set.</param>
        /// <param name="encoding">The character encoding to use when translating between
        ///     <see cref="String"/> and strings in Lua.  If <paramref name="encoding"/> is <c>null</c>,
        ///     iso-8859-1 is used.</param>
        [SecurityCritical]
        protected LuaBridge( IntPtr L, string clrBridge, Encoding encoding )
            : base(L, clrBridge, encoding)
        {
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="LuaBridge"/> and optionally releases the
        /// managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        [SecuritySafeCritical]
        protected override void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            _disposed = true;

            AssertEmptyStack();

            if (disposeManaged)
            {
                if (_state._objectTranslator != null)
                    _state._objectTranslator.Dispose();
            }

            base.Dispose(disposeManaged);
        }
    }
}
