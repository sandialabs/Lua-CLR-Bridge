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

    internal struct LuaState
    {
        internal readonly ObjectTranslator _objectTranslator;

        [SecurityCritical]
        private readonly IntPtr _L;

        [SecurityCritical]
        internal LuaState( ObjectTranslator objectTranslator, IntPtr L )
        {
            _objectTranslator = objectTranslator;

            _L = L;
        }

        [SecuritySafeCritical]
        public LockedLuaState Lock()
        {
            return new LockedLuaState(_objectTranslator, _L);
        }

        internal sealed class LockedLuaState : IDisposable
        {
            internal readonly ObjectTranslator _objectTranslator;

            // use of this value must be constrained to the lifetime of the lock object
            [SecurityCritical]
            internal readonly IntPtr _L;

            private bool _disposed = false;

            private bool _lockTaken = false;

            [SecurityCritical]
            internal LockedLuaState( ObjectTranslator objectTranslator, IntPtr L, bool @try = false )
            {
                try
                {
                    if (!@try)
                        objectTranslator.EnterLua(ref _lockTaken);
                    else
                        objectTranslator.TryEnterLua(ref _lockTaken);
                }
                catch
                {
                    if (_lockTaken)
                        objectTranslator.ExitLua();

                    throw;
                }

                _objectTranslator = objectTranslator;

                if (_lockTaken)
                    _L = L;
            }

#if DEBUG
            [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Only used for debugging.")]
            [SuppressMessage("Microsoft.Performance", "CA1821:RemoveEmptyFinalizers", Justification = "Conditionally compiled.")]
            ~LockedLuaState()
            {
                if (_objectTranslator != null)
                    throw new InvalidOperationException("Must dispose lock object!");
            }
#endif

            [SecuritySafeCritical]
            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (_lockTaken)
                    _objectTranslator.ExitLua();

#if DEBUG
                GC.SuppressFinalize(this);
#endif
            }
        }
    }
}
