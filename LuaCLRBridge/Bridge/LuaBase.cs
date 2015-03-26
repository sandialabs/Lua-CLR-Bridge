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
    /// The base class for representing Lua objects.
    /// </summary>
    /// <remarks>
    /// <see cref="LuaBase"/> keeps objects from being garbage-collected in Lua while they are reference in
    /// the CLR engine.
    /// </remarks>
    public abstract class LuaBase : MarshalByRefObject, IDisposable
    {
        [SecurityCritical]
        private static readonly int _refTable = LuaWrapper.LUA_REGISTRYINDEX;

        [SecurityCritical]
        private bool _disposed = false;

        /// <summary>
        /// The object translator that manages the Lua state that the object is in.
        /// </summary>
        [SecurityCritical]
        internal readonly ObjectTranslator _objectTranslator;

        [SecurityCritical]
        private readonly int _ref;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Security attribute.")]
        [SecuritySafeCritical]
        static LuaBase()
        {
            // exists solely for the security attribute
        }

        [SecurityCritical]
        internal LuaBase( ObjectTranslator objectTranslator, IntPtr L, int index )
        {
            _objectTranslator = objectTranslator;

            ObjectTranslator.CheckStack(L, 1);

            LuaWrapper.lua_pushvalue(L, index);
            _ref = LuaWrapper.luaL_ref(L, _refTable);
        }

        /// <summary>
        /// Ensures that the resources are freed and other cleanup operations are performed when the garbage
        /// collector reclaims the <see cref="LuaBase"/>.
        /// </summary>
        ~LuaBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all the resources used by the <see cref="LuaBase"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="LuaBase"/> and optionally releases the
        /// managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        [SecuritySafeCritical]
        protected virtual void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                using (var lockedMainL = _objectTranslator.TryLockedMainState)
                {
                    if (lockedMainL._L != IntPtr.Zero)
                    {
                        var L = lockedMainL._L;

                        LuaWrapper.luaL_unref(L, _refTable, _ref);
                    }
                    else
                    {
                        _objectTranslator.DeferUnref(_refTable, _ref);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // objectTranslator already disposed with Lua state
            }
        }

        /// <summary>
        /// Determines whether the object is raw-equal to another object according to Lua.
        /// </summary>
        /// <param name="obj">The object to be compared against.</param>
        /// <returns><c>true</c> if the objects are raw-equal according to Lua; otherwise, <c>false</c>.
        ///     </returns>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SecuritySafeCritical]
        public override bool Equals( object obj )
        {
            var objLuaBase = obj as LuaBase;
            if (objLuaBase != null && objLuaBase._objectTranslator != this._objectTranslator)
                return false;

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 2);  // self + obj

                Push(L); // self
                _objectTranslator.PushObject(L, obj);
                var result = LuaWrapper.lua_rawequal(L, -2, -1);
                LuaWrapper.lua_pop(L, 2);  // self, obj

                return result;
            }
        }

        /// <summary>
        /// Returns the hash code of the object as derived from Lua.
        /// </summary>
        /// <returns>The hash code of the object.</returns>
        [SecuritySafeCritical]
        public override int GetHashCode()
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                Push(L); // self
                IntPtr pointer = LuaWrapper.lua_topointer(L, -1);
                LuaWrapper.lua_pop(L, 1);  // self

                return pointer.GetHashCode();
            }
        }

        /// <summary>
        /// Returns the string that represents the object according to Lua.
        /// </summary>
        /// <returns>The string that represents the object according to Lua.</returns>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SecuritySafeCritical]
        public override string ToString()
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 3);  // stackCollector + luaToString + self

                LuaWrapper.lua_pushinteger(L, LuaWrapper.luaW_countlevels(L));
                LuaWrapper.lua_pushcclosure(L, _objectTranslator._stackCollector, 1);

                LuaWrapper.lua_pushcfunction(L, _objectTranslator._luaToString);

                Push(L); // self

                if (LuaWrapper.lua_pcall(L, 1, 1, -3) != LuaStatus.LUA_OK)
                {
                    object error = _objectTranslator.PopObject(L);

                    LuaWrapper.lua_pop(L, 1); // stackCollector

#if DEBUG
                    throw error as Exception ??
                        new LuaRuntimeException(error != null ? error.ToString() : "unspecified error");
#else
                    return null;
#endif
                }

                string result = LuaWrapper.lua_tostring(L, -1, _objectTranslator.Encoding);
                LuaWrapper.lua_pop(L, 1);

                LuaWrapper.lua_pop(L, 1); // stackCollector

                return result;
            }
        }

        [SecurityCritical]
        internal bool IsSameReference( LuaBase that )
        {
            return this._objectTranslator == that._objectTranslator &&
                this._ref == that._ref;
        }

        /// <summary>
        /// Pushes the represented object onto the stack of a specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <exception cref="ArgumentException">If the Lua state is not related to the Lua state that the
        ///     Lua object is in.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaBase"/> has been disposed.
        ///     </exception>
        /// <remarks>
        /// This method assumes that there is at least one free stack slot in the stack.
        /// </remarks>
        [SecurityCritical]
        internal void Push( IntPtr L )
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!_objectTranslator.HasSameMainState(L))
                throw new ArgumentException("Cannot transfer Lua objects between different states");

            LuaWrapper.lua_rawgeti(L, _refTable, _ref);
        }
    }
}
