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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Threading;
    using Lua;

    /// <summary>
    /// A helper class for <see cref="LuaBridge"/> that translates CLI objects for use in Lua.
    /// </summary>
    /// <remarks>
    /// <see cref="ObjectTranslator"/> holds the references for CLI objects that it translates so that they
    /// are not garbage collected until Lua collects them.  Therefore, it must not be disposed until after
    /// the Lua state is closed.
    /// </remarks>
    internal sealed partial class ObjectTranslator : MarshalByRefObject, IDisposable
    {
        [SecurityCritical]
        private bool _disposed = false;

        /// <summary>
        /// The main thread of the Lua state.
        /// </summary>
        [SecurityCritical]
        private LuaStateHandle _mainL;

        private readonly Encoding _encoding;

        private CLRBridge _clrBridge;

        /// <summary>
        /// The handles of CLI objects that are referenced by the Lua state and must not be collected by the
        /// CLR garbage collector.
        /// </summary>
        [SecurityCritical]
        private readonly HashSet<GCHandle> _handles = new HashSet<GCHandle>();

        /// <summary>
        /// The Lua objects that will be unreferenced when the Lua state is not in use.
        /// </summary>
        /// <remarks>
        /// When a <see cref="LuaBase"/> is finalized, it must unreference the Lua object so that the Lua
        /// object becomes eligible for collection by the Lua garbage collector.  The Lua state must be
        /// locked in order to unreference the object.  In order to avoid blocking the CLR finalizer thread,
        /// if the Lua state is locked when the finalizer runs then unreferencing is deferred until later
        /// (and on some other thread).
        /// </remarks>
        [SecurityCritical]
        private ConcurrentQueue<DeferredUnref> _deferredUnrefs = new ConcurrentQueue<DeferredUnref>();

        internal readonly LuaCFunction _atPanic;

        internal readonly LuaCFunction _stackCollector;

        internal readonly LuaCFunction _luaEquals;

        internal readonly LuaCFunction _luaToString;
        
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Security attribute.")]
        [SecuritySafeCritical]
        static ObjectTranslator()
        {
            // exists solely for the security attribute
        }

        [SecurityCritical]
        internal ObjectTranslator( LuaStateHandle mainL, Encoding encoding )
        {
            _mainL = mainL;
            _encoding = encoding;

            _atPanic = AtPanic;
            _stackCollector = StackCollector;

            _luaEquals = LuaEquals;
            _luaToString = LuaToString;

            var L = mainL.Handle;

            InitializeObjectUserDatas(L);

            InitializeMetamethods(L);

            InitializeLuaFunctionDelegates(L);
        }

        ~ObjectTranslator()
        {
            Dispose(false);
        }

        internal LuaState.LockedLuaState LockedMainState
        {
            [SecurityCritical]
            get
            {
                return new LuaState.LockedLuaState(this, _mainL.Handle);
            }
        }

        internal LuaState.LockedLuaState TryLockedMainState
        {
            [SecurityCritical]
            get
            {
                return new LuaState.LockedLuaState(this, _mainL.Handle, @try: true);
            }
        }

        /// <summary>
        /// Gets the character encoding used when translating between <see cref="String"/> and strings in Lua.
        /// </summary>
        internal Encoding Encoding
        {
            get { return _encoding; }
        }

        internal CLRBridge CLRBridge
        {
            get
            {
                return _clrBridge ?? (_clrBridge = new CLRBridge(this));
            }
        }

        /// <summary>
        /// Releases all the resources used by the <see cref="ObjectTranslator"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        [SecuritySafeCritical]
        private void Dispose( bool disposeManaged )
        {
            /* Lock to ensure that no other thread is using the Lua state.
               We have no choice but to wait for this lock.  It is unlikely that this will
               ever block, however. */

            bool lockTaken = false;
            try
            {
                Monitor.Enter(this, ref lockTaken);

                if (_disposed)
                    return;

                _disposed = true;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(this);
            }

            if (_mainL != null && !_mainL.IsClosed)
                _mainL.Close();

            if (_handles != null)
            {
                Debug.Assert(_handles.Count == 0, "Lua state should be closed which should release all handles.");

                foreach (GCHandle handle in _handles)
                    handle.Free();
            }

            if (_objectUserDataRefs != null)
                Debug.Assert(_objectUserDataRefs.Count == 0, "Lua state should be closed which should release all refs.");

            if (disposeManaged)
            {
                if (_clrBridge != null)
                    _clrBridge.Dispose();

                if (_objectUserDatas != null)
                    _objectUserDatas.Dispose();

                if (_luaFunctionDelegates != null)
                    _luaFunctionDelegates.Dispose();
            }
        }

        [SecurityCritical]
        internal bool HasSameMainState( IntPtr L )
        {
            return GetMainL(L) == _mainL.Handle;
        }

        [SecurityCritical]
        internal static IntPtr GetMainL( IntPtr L )
        {
            ObjectTranslator.CheckStack(L, 1);

            LuaWrapper.lua_pushinteger(L, LuaWrapper.LUA_RIDX_MAINTHREAD);
            LuaWrapper.lua_gettable(L, LuaWrapper.LUA_REGISTRYINDEX);
            IntPtr mainL = LuaWrapper.lua_tothread(L, -1);
            LuaWrapper.lua_pop(L, 1);

            return mainL;
        }

        /// <summary>
        /// Attempts to lock the <see cref="ObjectTranslator"/>.
        /// </summary>
        /// <param name="lockTaken">The result of attempting to acquire the lock.  The input must be
        ///     <c>false</c>.  The output will be <c>true</c> if the lock is acquired; otherwise
        ///     <c>false</c>.  The output may be <c>true</c> even if an exception is thrown.</param>
        /// <exception cref="ArgumentException"><paramref name="lockTaken"/> is <c>false</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ObjectTranslator"/> is disposed.
        ///     </exception>
        /// <remarks>
        /// This is the beginning boundary for allowing access to the Lua state.
        /// </remarks>
        [SecurityCritical]
        internal void EnterLua( ref bool lockTaken )
        {
            Monitor.Enter(this, ref lockTaken);

            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Attempts to lock the <see cref="ObjectTranslator"/> without blocking.
        /// </summary>
        /// <param name="lockTaken">The result of attempting to acquire the lock.  The input must be
        ///     <c>false</c>.  The output will be <c>true</c> if the lock is acquired; otherwise
        ///     <c>false</c>.  The output may be <c>true</c> even if an exception is thrown.</param>
        /// <exception cref="ArgumentException"><paramref name="lockTaken"/> is <c>false</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ObjectTranslator"/> is disposed.
        ///     </exception>
        /// <remarks>
        /// This is the beginning boundary for allowing access to the Lua state.
        /// </remarks>
        [SecurityCritical]
        internal void TryEnterLua( ref bool lockTaken )
        {
            Monitor.TryEnter(this, ref lockTaken);

            if (lockTaken && _disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Unlocks the <see cref="ObjectTranslator"/>.
        /// </summary>
        /// <remarks>
        /// This is the ending boundary for allowing access to the Lua state.
        /// </remarks>
        [SecurityCritical]
        internal void ExitLua()
        {
            if (!_disposed)
            {
                DeferredUnref deferredUnref;
                while (_deferredUnrefs.TryDequeue(out deferredUnref))
                    LuaWrapper.luaL_unref(_mainL.Handle, deferredUnref.Table, deferredUnref.Index);
            }

            Monitor.Exit(this);
        }

        [SecurityCritical]
        internal void DeferUnref( int table, int index )
        {
            _deferredUnrefs.Enqueue(new DeferredUnref(table, index));
        }

        [SecurityCritical]
        internal static void CheckStack( IntPtr L, int extra )
        {
            if (!LuaWrapper.lua_checkstack(L, extra))
                throw new LuaRuntimeException("Insufficient stack");
        }

        /// <summary>
        /// Pushes an object onto the Lua stack, translating it to a comparable Lua value if possible.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="o">The object to push.</param>
        /// <remarks>
        /// This method assumes that there is at least one free stack slot in the stack.
        /// </remarks>
        [SecurityCritical]
        internal void PushObject( IntPtr L, object o )
        {
            if (o == null)
            {
                LuaWrapper.lua_pushnil(L);
                return;
            }

            Type type = o.GetType();

            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        LuaWrapper.lua_pushboolean(L, (Boolean)o);
                        break;

                    case TypeCode.Double:
                        LuaWrapper.lua_pushnumber(L, (Double)o);
                        break;
                    case TypeCode.Single:
                        LuaWrapper.lua_pushnumber(L, (Single)o);
                        break;
                    case TypeCode.Int64:
                        // 64-bit integers get special treatment because they cannot be exactly represented by a 64-bit float
                        PushUntranslatedObject(L, new CLRInt64((Int64)o));
                        break;
                    case TypeCode.UInt64:
                        // 64-bit integers get special treatment because they cannot be exactly represented by a 64-bit float
                        PushUntranslatedObject(L, new CLRUInt64((UInt64)o));
                        break;
                    case TypeCode.Int32:
                        LuaWrapper.lua_pushnumber(L, (Int32)o);
                        break;
                    case TypeCode.UInt32:
                        LuaWrapper.lua_pushnumber(L, (UInt32)o);
                        break;
                    case TypeCode.Int16:
                        LuaWrapper.lua_pushnumber(L, (Int16)o);
                        break;
                    case TypeCode.UInt16:
                        LuaWrapper.lua_pushnumber(L, (UInt16)o);
                        break;
                    case TypeCode.SByte:
                        LuaWrapper.lua_pushnumber(L, (SByte)o);
                        break;
                    case TypeCode.Byte:
                        LuaWrapper.lua_pushnumber(L, (Byte)o);
                        break;

                    case TypeCode.Char:
                        LuaWrapper.lua_pushnumber(L, (Char)o);
                        break;

                    default:
                        if (o is IntPtr)
                            LuaWrapper.lua_pushlightuserdata(L, (IntPtr)o);
                        else
                            throw new InvalidOperationException("Should never happen!");
                        break;
                }
            }
            else if (o is string)
            {
                LuaWrapper.lua_pushstring(L, o as string, _encoding);
            }
            else if (o is LuaBase) // table, function, thread, userdata
            {
                (o as LuaBase).Push(L);
            }
            else if (o is LuaCFunction)
            {
                PushCFunctionDelegate(L, o as LuaCFunction);
            }
            else if (o is LuaSafeCFunction)
            {
                PushCFunctionDelegate(L, o as LuaSafeCFunction);
            }
            else
            {
                PushUntranslatedObject(L, o);
            }
        }

        [SecurityCritical]
        internal object ToObject( IntPtr L, int index = -1 )
        {
            switch (LuaWrapper.lua_type(L, index))
            {
                case LuaType.LUA_TNONE:
                    throw new InvalidOperationException("Cannot translate empty stack slot");

                case LuaType.LUA_TNIL:
                    return null;

                case LuaType.LUA_TBOOLEAN:
                    return LuaWrapper.lua_toboolean(L, index);

                case LuaType.LUA_TLIGHTUSERDATA:
                    return LuaWrapper.lua_touserdata(L, index);

                case LuaType.LUA_TNUMBER:
                    return LuaWrapper.lua_tonumber(L, index);

                case LuaType.LUA_TSTRING:
                    UIntPtr len;
                    return LuaWrapper.lua_tolstring(L, index, out len, _encoding);

                case LuaType.LUA_TTABLE:
                    return new LuaTable(this, L, index);

                case LuaType.LUA_TFUNCTION:
                    return new LuaFunction(this, L, index);

                case LuaType.LUA_TUSERDATA:
                    return ToUntranslatedObject(L, index) ?? new LuaUserData(this, L, index);

                case LuaType.LUA_TTHREAD:
                    return new LuaThread(this, L, index);

                default:
                    throw new InvalidOperationException("Should never happen!");
            }
        }

        [SecurityCritical]
        internal object PopObject( IntPtr L )
        {
            object result = ToObject(L);
            LuaWrapper.lua_pop(L, 1);
            return result;
        }

        /// <summary>
        /// Pushes a userdata that represents a CLI object onto the stack of the specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="o">The object to be pushed onto the stack.</param>
        [SecurityCritical]
        internal void PushUntranslatedObject( IntPtr L, object o )
        {
            PushUntranslatedObject(L, o, _objectMetatableName);
        }

        [SecurityCritical]
        private void PushUntranslatedObject( IntPtr L, object o, string metatableName )
        {
            bool isRefType = !o.GetType().IsValueType;

            if (isRefType && PushObjectUserData(L, o))
                return;

            CheckStack(L, 2);  // udata + metatable

            GCHandle handle = GCHandle.Alloc(o);
            _handles.Add(handle);

            IntPtr udata = LuaWrapper.lua_newuserdata(L, (uint)Marshal.SizeOf(handle));
            Marshal.StructureToPtr(handle, udata, false);

            LuaWrapper.luaL_getmetatable(L, metatableName, _encoding);
            LuaWrapper.lua_setmetatable(L, -2);

            if (isRefType)
                StoreObjectUserData(L, o, udata);
        }

        /// <summary>
        /// Retrieves a CLI object from a location in the stack of the specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="index">The index in the stack.</param>
        /// <returns>The CLI object at the specified index if it is a CLI object; otherwise, null.</returns>
        [SecurityCritical]
        internal object ToUntranslatedObject( IntPtr L, int index )
        {
            return ToUntranslatedObject(L, index, _objectMetatableName);
        }

        [SecurityCritical]
        private object ToUntranslatedObject( IntPtr L, int index, string metatableName )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, index, metatableName, _encoding);
            if (udata == IntPtr.Zero)
                return null;

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            Debug.Assert(_handles.Contains(handle), "Object handle should still exist.");

            // 64-bit numbers need to be unwrapped
            if (handle.Target is CLRInt64)
                return ((CLRInt64)handle.Target)._value;
            else if (handle.Target is CLRUInt64)
                return ((CLRUInt64)handle.Target)._value;
            else
                return handle.Target;
        }

        /// <summary>
        /// Pushes a CLI delegate of a cfunction (via a proxy delegate that handles exceptions) onto the
        /// stack of the specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="function">The cfunction to be pushed onto the stack.</param>
        [SecurityCritical]
        internal void PushCFunctionDelegate( IntPtr L, LuaCFunction function )
        {
            PushCFunctionDelegate(L, new LuaFunction.LuaCFunctionProxy(this, function));
        }

        /// <summary>
        /// Pushes a CLI delegate of a safe cfunction (via a proxy delegate that handles exceptions) onto
        /// the stack of the specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="function">The safe cfunction to be pushed onto the stack.</param>
        [SecurityCritical]
        internal void PushCFunctionDelegate( IntPtr L, LuaSafeCFunction function )
        {
            PushCFunctionDelegate(L, new LuaFunction.LuaSafeCFunctionProxy(this, function));
        }

        [SecurityCritical]
        private void PushCFunctionDelegate( IntPtr L, LuaFunction.LuaFunctionProxy functionProxy )
        {
            CheckStack(L, 2);  // funtionProxy + functionProxyDelegate

            // keep proxy as an upvalue in order to be able to retrieve original delegate
            PushUntranslatedObject(L, functionProxy);

            var functionProxyDelegate = new LuaCFunction(functionProxy.Call);

            // keep proxy delegate as an upvalue to prevent it from being collected
            PushUntranslatedObject(L, functionProxyDelegate);

            LuaWrapper.lua_pushcclosure(L, functionProxyDelegate, 2);
        }

        /// <summary>
        /// Retrieves the CLI delegate of a cfunction (or creates a delegate if none exists) from a
        /// location in the stack of the specified Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="index">The index in the stack.</param>
        /// <returns>The delegate for the cfunction at the specified index if it is a cfunction;
        ///     otherwise, null.</returns>
        [SecurityCritical]
        internal Delegate ToCFunctionDelegate( IntPtr L, int index )
        {
            if (!LuaWrapper.lua_iscfunction(L, index))
                return null;

            CheckStack(L, 1);

            if (LuaWrapper.lua_getupvalue(L, index, 1, _encoding) != null)
            {
                var functionProxy = ToUntranslatedObject(L, -1) as LuaFunction.LuaFunctionProxy;
                LuaWrapper.lua_pop(L, 1);

                // if cfunction was created from a delegate then we must use that delegate
                if (functionProxy != null)
                    return functionProxy.Delegate;
            }

            IntPtr cfunction = LuaWrapper.lua_tocfunction(L, index);
            return (LuaCFunction)Marshal.GetDelegateForFunctionPointer(cfunction, typeof(LuaCFunction));
        }

        /// <summary>
        /// The metatable function for releasing a CLI object for collection when it has been collected in
        /// Lua.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="metatableName">The expected name of the type of object being collected.</param>
        /// <returns>The number of return values on the Lua stack.</returns>
        [SecurityCritical]
        private int GarbageCollect( IntPtr L, string metatableName )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, metatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            /* Ensure that the userdata cannot be used after being garbage collected.  (Yes, this is possible.
             * The userdata may be an upvalue of the __gc function of another object being collected.) */

            CheckStack(L, 1);  // nil

            LuaWrapper.lua_pushnil(L);
            LuaWrapper.lua_setmetatable(L, -2);

            // Release the CLI object reference.

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            Debug.Assert(_handles.Contains(handle), "Object handle should still exist.");

            ReleaseObjectUserData(handle.Target, udata);

            if (_handles.Remove(handle))
                handle.Free();

            return 0;
        }

        [SecurityCritical]
        internal int Throw( IntPtr L, Exception ex )
        {
            LuaWrapper.lua_settop(L, 0);
            LuaWrapper.luaL_checkstack(L, 1, null, Encoding);

            PushUntranslatedObject(L, ex);
            return LuaWrapper.lua_error(L);
        }

        [SecurityCritical]
        private int AtPanic( IntPtr L )
        {
            // preferably we check for problems and throw our own exceptions rather than ending up here
#if DEBUG
            new PermissionSet(System.Security.Permissions.PermissionState.Unrestricted).Assert();
            Debug.Assert(false, "Don't panic.");
            PermissionSet.RevertAssert();
#endif

            ObjectTranslator.StackCollector(L, this);

            object error = PopObject(L);

            LuaWrapper.lua_settop(L, 0);

            throw error as Exception ??
                new LuaPanicException(error != null ? error.ToString() : "unspecified error");
        }

        [SecurityCritical]
        private int StackCollector( IntPtr L )
        {
            return StackCollector(L, this);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Throwing from a Lua message handler accomplishes nothing.")]
        [SecurityCritical]
        private static int StackCollector( IntPtr L, ObjectTranslator objectTranslator )
        {
            Debug.Assert(objectTranslator.HasSameMainState(L), "Stack collector invoked in unrelated Lua state.");

            object error = objectTranslator.PopObject(L);

            try
            {
                if (error is string)
                    error = new LuaRuntimeException(error as string);

                if (error is Exception)
                {
                    Exception exception = error as Exception;

                    int bottom = LuaWrapper.lua_tointeger(L, LuaWrapper.lua_upvalueindex(1)) + 1;

                    CheckStack(L, 1);

                    LuaWrapper.luaW_traceback(L, L, 1, bottom);
                    string stackTrace = LuaWrapper.lua_tostring(L, -1, objectTranslator.Encoding) + Environment.NewLine;
                    LuaWrapper.lua_pop(L, 1); // traceback

                    string garbage = "stack traceback:\n";
                    if (stackTrace.StartsWith(garbage, StringComparison.Ordinal))
                        stackTrace = stackTrace.Substring(garbage.Length);

                    exception.PreserveStackTrace();
                    exception.AppendPreservedStackTrace(stackTrace);
                }
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception)
            {
                // throwing from message handler is unproductive
            }

            LuaWrapper.lua_settop(L, 0);
            LuaWrapper.luaL_checkstack(L, 1, null, objectTranslator.Encoding);

            objectTranslator.PushObject(L, error);

            return 1;
        }

        [SecurityCritical]
        private int LuaEquals( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.luaL_checkany(L, 2);

            bool result = LuaWrapper.lua_compare(L, 1, 2, LuaCompareOp.LUA_OPEQ);

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            LuaWrapper.lua_pushboolean(L, result);
            return 1;
        }

        [SecurityCritical]
        private int LuaToString( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);

            UIntPtr len;
            string result = LuaWrapper.luaL_tolstring(L, 1, out len, _encoding);
            return 1;
        }

        private struct DeferredUnref
        {
            public readonly int Table;
            public readonly int Index;

            public DeferredUnref( int table, int index )
            {
                this.Table = table;
                this.Index = index;
            }
        }
    }
}
