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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using Lua;

    /// <summary>
    /// Represents a thread of a Lua state.
    /// </summary>
    public abstract class LuaBridgeBase : MarshalByRefObject, IDisposable
    {
        [SecurityCritical]
        private static readonly Dictionary<string, IntPtr> _libs = new Dictionary<string, IntPtr>
        {
            { "_G",                       LuaWrapper.luaopen_base },
            { LuaWrapper.LUA_COLIBNAME,   LuaWrapper.luaopen_coroutine },
            { LuaWrapper.LUA_TABLIBNAME,  LuaWrapper.luaopen_table },
            { LuaWrapper.LUA_IOLIBNAME,   LuaWrapper.luaopen_io },
            { LuaWrapper.LUA_OSLIBNAME,   LuaWrapper.luaopen_os },
            { LuaWrapper.LUA_STRLIBNAME,  LuaWrapper.luaopen_string },
            { LuaWrapper.LUA_BITLIBNAME,  LuaWrapper.luaopen_bit32 },
            { LuaWrapper.LUA_MATHLIBNAME, LuaWrapper.luaopen_math },
            { LuaWrapper.LUA_DBLIBNAME,   LuaWrapper.luaopen_debug },
            { LuaWrapper.LUA_LOADLIBNAME, LuaWrapper.luaopen_package },
        };

        private bool _disposed = false;

        [SecurityCritical]
        internal readonly LuaState _state;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Security attribute.")]
        [SecuritySafeCritical]
        static LuaBridgeBase()
        {
            // exists solely for the security attribute
        }

        [SecurityCritical]
        internal LuaBridgeBase( IntPtr L, string clrBridge, Encoding encoding )
            : this(L, new ObjectTranslator(new LuaStateHandle(L), encoding ?? Encoding.GetEncoding(28591 /* iso-8859-1 */)), clrBridge)
        {
            // nothing to do
        }

        [SecurityCritical]
        internal LuaBridgeBase( IntPtr L, ObjectTranslator objectTranslator, string clrBridge )
        {
            _state = new LuaState(objectTranslator, L);

            LuaWrapper.lua_atpanic(L, Marshal.GetFunctionPointerForDelegate(objectTranslator._atPanic));

            if (clrBridge != String.Empty)
                this[clrBridge ?? "CLR"] = objectTranslator.CLRBridge;
        }

        /// <summary>
        /// Ensures that the resources are freed and other cleanup operations are performed when the garbage
        /// collector reclaims the <see cref="LuaBridgeBase"/>.
        /// </summary>
        ~LuaBridgeBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the character encoding used when translating between <see cref="String"/> and strings in
        /// Lua.
        /// </summary>
        /// <remarks>
        /// The default encoding is "iso-8859-1" because it allows for strings to be used to transfer binary
        /// data to/from Lua.
        /// </remarks>
        public Encoding Encoding
        {
            [SecuritySafeCritical]
            get { return _state._objectTranslator.Encoding; }
        }

        /// <summary>
        /// Gets or sets the global variable table.
        /// </summary>
        public LuaTable Environment
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedL = LockedState)
                {
                    var L = lockedL._L;
                    var objectTranslator = lockedL._objectTranslator;

                    ObjectTranslator.CheckStack(L, 1);

                    LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_GLOBALS);
                    LuaWrapper.lua_gettable(L, LuaWrapper.LUA_REGISTRYINDEX);

                    return objectTranslator.PopObject(L) as LuaTable;
                }
            }

            [SecuritySafeCritical]
            set
            {
                using (var lockedL = LockedState)
                {
                    var L = lockedL._L;

                    ObjectTranslator.CheckStack(L, 2);  // key + value

                    LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_GLOBALS);
                    value.Push(L);
                    LuaWrapper.lua_settable(L, LuaWrapper.LUA_REGISTRYINDEX);
                }
            }
        }

        /// <summary>
        /// Gets the lock object that must be held in order to access the Lua state.  The lock object must
        /// be disposed when access to the Lua state is no longer required.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaBridgeBase"/> has been disposed.
        ///     </exception>
        internal LuaState.LockedLuaState LockedState
        {
            [SecurityCritical]
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _state.Lock();
            }
        }

        /// <summary>
        /// Gets or sets global Lua variables.
        /// </summary>
        /// <param name="global">The global variable name.</param>
        /// <returns>The value of the global variable if it exists; otherwise, <c>null</c>.</returns>
        public object this[ string global ]
        {
            [SecuritySafeCritical]
            get
            {
                using (var lockedL = LockedState)
                {
                    var L = lockedL._L;
                    var objectTranslator = lockedL._objectTranslator;

                    ObjectTranslator.CheckStack(L, 1);

                    LuaWrapper.lua_getglobal(L, global, Encoding);
                    return objectTranslator.PopObject(L);
                }
            }

            [SecuritySafeCritical]
            set
            {
                using (var lockedL = LockedState)
                {
                    var L = lockedL._L;
                    var objectTranslator = lockedL._objectTranslator;

                    ObjectTranslator.CheckStack(L, 1);

                    objectTranslator.PushObject(L, value);
                    LuaWrapper.lua_setglobal(L, global, Encoding);
                }
            }
        }

        /// <summary>
        /// Releases all the resources used by the <see cref="LuaBridgeBase"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="LuaBridgeBase"/> and optionally releases
        /// the managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        [SecuritySafeCritical]
        protected virtual void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        /// <summary>
        /// Executes a Lua text chunk.
        /// </summary>
        /// <param name="buff">The Lua chunk.</param>
        /// <param name="name">The name of the chunk (used in error messages).</param>
        /// <returns>The return values from executing the chunk.</returns>
        /// <exception cref="LuaCompilerException">If there was a Lua error while compiling the chunk.
        ///     </exception>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the chunk.
        ///     </exception>
        public object[] Do( string buff, string name = "<string>" )
        {
            return Load(buff, name).Call(this);
        }

        /// <summary>
        /// Loads a Lua text chunk.
        /// </summary>
        /// <param name="buff">The Lua chunk.</param>
        /// <param name="name">The name of the chunk (used in error messages).</param>
        /// <returns>The Lua function that will execute the chunk.</returns>
        /// <exception cref="LuaCompilerException">If there was a Lua error while compiling the chunk.
        ///     </exception>
        [SecuritySafeCritical]
        public LuaFunction Load( string buff, string name = "<string>" )
        {
            using (var lockedL = LockedState)
            {
                var L = lockedL._L;
                var objectTranslator = lockedL._objectTranslator;

                if (LuaWrapper.luaW_loadbufferx(L, buff, name, "t", Encoding, Encoding) != LuaStatus.LUA_OK)
                {
                    object error = objectTranslator.PopObject(L);
                    Debug.Assert(!(error is Exception), "Loading Lua script string should only produce Lua error.");
                    throw new LuaCompilerException(error.ToString());
                }

                LuaFunction f = new LuaFunction(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1);

                return f;
            }
        }

        /// <summary>
        /// Loads a Lua chunk.
        /// </summary>
        /// <param name="stream">The stream from which to load the Lua chunk.</param>
        /// <param name="name">The name of the chunk (used in error messages).</param>
        /// <param name="mode">The acceptable chunk formats ("t" for text, "b" for binary, or "bt" for
        ///     either).</param>
        /// <returns>The Lua function that will execute the chunk.</returns>
        /// <exception cref="LuaCompilerException">If there was a Lua error while compiling the chunk.
        ///     </exception>
        [SecuritySafeCritical]
        public LuaFunction Load( Stream stream, string name = "<stream>", string mode = "bt" )
        {
            using (var lockedL = LockedState)
            {
                var L = lockedL._L;
                var objectTranslator = lockedL._objectTranslator;

                using (var streamReader = new LuaStreamReader(stream))
                {
                    if (LuaWrapper.lua_load(L, streamReader.Reader, IntPtr.Zero, name, mode, Encoding) != LuaStatus.LUA_OK)
                    {
                        object error = objectTranslator.PopObject(L);
                        Debug.Assert(!(error is Exception), "Loading Lua script string should only produce Lua error.");
                        throw new LuaCompilerException(error.ToString());
                    }

                    LuaFunction f = new LuaFunction(objectTranslator, L, -1);
                    LuaWrapper.lua_pop(L, 1);

                    return f;
                }
            }
        }

        /// <summary>
        /// Loads a Lua built-in library.
        /// </summary>
        /// <param name="name">The name of the library; "_G" for the base library.</param>
        /// <returns><c>false</c> if library failed to load; otherwise, <c>true</c>.</returns>
        [SecuritySafeCritical]
        public bool LoadLib( string name )
        {
            using (var lockedL = LockedState)
            {
                var L = lockedL._L;

                IntPtr luaopen;
                if (!_libs.TryGetValue(name, out luaopen))
                    return false;

                LuaWrapper.luaL_requiref(L, name, luaopen, true, Encoding);
                LuaWrapper.lua_pop(L, 1);

                return true;
            }
        }

        #region Lua value creators

        /* These creator functions exist rather than having constructors on the Lua value types so that
         * values can be constructed across AppDomain boundaries.  The Lua values have to be constructed in
         * the same AppDomain as the LuaBridge, so constructors won't work. */

        /// <summary>
        /// Creates a new <see cref="LuaFunction"/> from a specified <see cref="LuaCFunction"/>.
        /// </summary>
        /// <param name="function">The function from which the Lua function will be created.</param>
        /// <returns>The new Lua function.</returns>
        [SecurityCritical]
        public LuaFunction NewFunction( LuaCFunction function )
        {
            return LuaFunction.Create(_state._objectTranslator, function);
        }

        /// <summary>
        /// Creates a new <see cref="LuaFunction"/> from a specified <see cref="LuaSafeCFunction"/>.
        /// </summary>
        /// <param name="function">The function from which the Lua function will be created.</param>
        /// <returns>The new Lua function.</returns>
        [SecuritySafeCritical]
        public LuaFunction NewFunction( LuaSafeCFunction function )
        {
            return LuaFunction.Create(_state._objectTranslator, function);
        }

        /// <summary>
        /// Creates a new Lua table.
        /// </summary>
        /// <returns>The new Lua table.</returns>
        [SecuritySafeCritical]
        public LuaTable NewTable()
        {
            return LuaTable.Create(_state._objectTranslator);
        }

        /// <summary>
        /// Creates a new Lua table with allocation hints.
        /// </summary>
        /// <param name="arrayCountHint">The number of array entries to allocate.</param>
        /// <param name="recordCountHint">The number of record entries to allocate.</param>
        /// <returns>The new Lua table.</returns>
        [SecuritySafeCritical]
        public LuaTable NewTable( int arrayCountHint, int recordCountHint )
        {
            return LuaTable.Create(_state._objectTranslator, arrayCountHint, recordCountHint);
        }

        /// <summary>
        /// Creates a new Lua thread.
        /// </summary>
        /// <returns>The new Lua thread.</returns>
        [SecuritySafeCritical]
        public LuaThread NewThread()
        {
            return LuaThread.Create(_state._objectTranslator);
        }

        #endregion

        /// <summary>
        /// Performs a full garbage-collection cycle.
        /// </summary>
        [SecuritySafeCritical]
        public void CollectGarbage()
        {
            using (var lockedL = LockedState)
            {
                var L = lockedL._L;

                LuaWrapper.lua_gc(L, LuaGCOption.LUA_GCCOLLECT, 0);
            }
        }

        /// <summary>
        /// Pushes a specified CLI object onto the stack of a specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="object">The object to be pushed.</param>
        /// <remarks>
        /// Be sure to leave the stack with the same number of elements as you found it.
        /// </remarks>
        /// <exception cref="LuaRuntimeException">The size of the Lua stack is insufficient </exception>
        [SecurityCritical]
        public void PushObject( IntPtr L, object @object )
        {
            ObjectTranslator.CheckStack(L, 1);

            _state._objectTranslator.PushObject(L, @object);
        }

        /// <summary>
        /// Gets an object from the stack of a specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="index">The index in the stack of the item to return.</param>
        /// <returns>The object at the specified index.</returns>
        [SecurityCritical]
        public object ToObject( IntPtr L, int index = -1 )
        {
            return _state._objectTranslator.ToObject(L, index);
        }

        /// <summary>
        /// Pops an object from the stack of a specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The object that was on the top of the stack.</returns>
        /// <remarks>
        /// Be sure to leave the stack with the same number of elements as you found it.
        /// </remarks>
        [SecurityCritical]
        public object PopObject( IntPtr L )
        {
            object result = ToObject(L);
            LuaWrapper.lua_pop(L, 1);
            return result;
        }

        /// <summary>
        /// Asserts that the Lua stack is empty.
        /// </summary>
        [Conditional("DEBUG")]
        [SecuritySafeCritical]
        protected void AssertEmptyStack()
        {
            try
            {
                if (_state._objectTranslator != null)
                {
                    using (var lockedL = LockedState)
                    {
                        var L = lockedL._L;

                        Debug.Assert(LuaWrapper.lua_gettop(L) == 0, "Thread stack should be empty at disposal.");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // objectTranslator already disposed with Lua state
            }
        }
    }
}
