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
    using System.Text;
    using Lua;

    /// <summary>
    /// Encapsulates a method for use with <see cref="InstrumentedLuaBridge.Interject"/>.
    /// </summary>
    /// <param name="bridge">The bridge of the main Lua thread.</param>
    public delegate void Interjection( LuaBridge bridge );

    /// <summary>
    /// Specifies how a Lua bridge will be instrumented.
    /// </summary>
    [Flags]
    public enum Instrumentations
    {
        /// <summary>No instrumentation will occur.</summary>
        None = 0,

        /// <summary>A hook for interrupting script execution will be set.</summary>
        Interruption,

        /// <summary>Memory allocation will be monitored.</summary>
        MemoryMonitoring,
    }

    /// <summary>
    /// Represents the main thread of a Lua state with optional instrumentation.
    /// </summary>
    public sealed class InstrumentedLuaBridge : LuaBridge
    {
        private bool _disposed = false;

        [SecurityCritical]
        private readonly LuaAllocTracker _allocTracker;

        private readonly LuaInterjector _interjector;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedLuaBridge"/> class with a new Lua state
        /// with optional instrumentation.
        /// </summary>
        /// <param name="instrumentations">The types of instrumentation that will be added to the state.
        ///     </param>
        /// <param name="clrBridge">The global variable that will be set to the interface used in Lua to
        ///     access the CLR.  If <paramref name="clrBridge"/> is <see cref="String.Empty"/>, no global
        ///     variable is set.  If <paramref name="clrBridge"/> is <c>null</c>, the global variable "CLR"
        ///     is set.</param>
        /// <param name="encoding">The character encoding to use when translating between
        ///     <see cref="String"/> and strings in Lua.  If <paramref name="encoding"/> is <c>null</c>,
        ///     iso-8859-1 is used.</param>
        [SecuritySafeCritical]
        public InstrumentedLuaBridge( Instrumentations instrumentations, string clrBridge = null, Encoding encoding = null )
            : this(instrumentations.HasFlag(Instrumentations.MemoryMonitoring), null, clrBridge, encoding)
        {
            if (instrumentations.HasFlag(Instrumentations.Interruption))
            {
                using (var lockedL = LockedState)
                {
                    var L = lockedL._L;

                    _interjector = LuaHelper.luaH_setnewinterjectionhook(L);
                }
            }
        }

        [SecuritySafeCritical]
        private InstrumentedLuaBridge( bool instrumentMemory, LuaAllocTracker allocTracker, string clrBridge, Encoding encoding )
            : base(instrumentMemory ? LuaHelper.luaH_newstate(out allocTracker) : LuaWrapper.luaL_newstate(), clrBridge, encoding)
        {
            _allocTracker = allocTracker;
        }

        /// <summary>
        /// Gets the size of memory currently allocated for the Lua state.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the
        ///     <see cref="Instrumentations.MemoryMonitoring"/> flag was not specified at construction.
        ///     </exception>
        [CLSCompliant(false)]
        public UIntPtr MemoryAllocatedSize
        {
            [SecuritySafeCritical]
            get
            {
                if (_allocTracker == null)
                    throw new InvalidOperationException();
                else
                    return _allocTracker.Allocated;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="InstrumentedLuaBridge"/> and optionally
        /// releases the managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        [SecuritySafeCritical]
        protected override void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            _disposed = true;

            base.Dispose(disposeManaged);

            if (_allocTracker != null)
                Debug.Assert(MemoryAllocatedSize == UIntPtr.Zero, "Allocated memory should be zero at disposal.");
        }

        /// <summary>
        /// Permanently cancels execution in the main thread of the Lua state.
        /// </summary>
        /// <param name="message">The Lua error message that will be propagated during cancellation.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="Instrumentations.Interruption"/>
        ///     flag was not specified at construction.</exception>
        [SecuritySafeCritical]
        public void Cancel( string message )
        {
            if (_interjector == null)
                throw new InvalidOperationException();
            else
                _interjector.Cancel(message, Encoding);
        }

        /// <summary>
        /// Runs a delegate in the main thread of the Lua state as soon as possible.
        /// </summary>
        /// <param name="interjection">The delegate that will be run.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="Instrumentations.Interruption"/>
        ///     flag was not specified at construction.</exception>
        /// <remarks>
        /// Interjections will be run in the order that they were interjected.  Interjections run within a
        /// Lua debug hook and therefore are not interruptible.
        /// </remarks>
        [SecuritySafeCritical]
        public void Interject( Interjection interjection )
        {
            if (_interjector == null)
                throw new InvalidOperationException();
            else
                _interjector.Interject(( L ) => interjection(this));
        }
    }
}
