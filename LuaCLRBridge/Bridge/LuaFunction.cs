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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using Lua;

    /// <summary>
    /// Encapsulates a safely wrapped C function passed to Lua.
    /// </summary>
    /// <param name="bridge">The bridge that the function was transferred across.</param>
    /// <param name="args">The function arguments.</param>
    /// <returns>The function return values.</returns>
    public delegate object[] LuaSafeCFunction( LuaBridgeBase bridge, object[] args );

    /// <summary>
    /// Represents a Lua function.
    /// </summary>
    public class LuaFunction : LuaFunctionBase
    {
        [SecurityCritical]
        internal LuaFunction( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
        }

        /// <summary>
        /// Dumps a Lua function as a binary chunk.
        /// </summary>
        /// <param name="stream">The stream into which to dump the Lua chunk.</param>
        [SecuritySafeCritical]
        public void Dump( Stream stream )
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                Push(L); // self

                var streamWriter = new LuaStreamWriter(stream);

                LuaWrapper.lua_dump(L, streamWriter.Writer, IntPtr.Zero);

                LuaWrapper.lua_pop(L, 1); // self
            }
        }

        /// <summary>
        /// Creates a new <see cref="LuaFunction"/> from a specified <see cref="LuaCFunction"/>.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the Lua state that the
        ///     function will exist within.</param>
        /// <param name="function">The function from which the Lua function will be created.</param>
        /// <returns>The new Lua function.</returns>
        [SecurityCritical]
        internal static LuaFunction Create( ObjectTranslator objectTranslator, LuaCFunction function )
        {
            using (var lockedMainL = objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                objectTranslator.PushCFunctionDelegate(L, function);
                LuaFunction luaFunction = new LuaFunction(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1); // function

                return luaFunction;
            }
        }

        /// <summary>
        /// Creates a new <see cref="LuaFunction"/> from a specified <see cref="LuaSafeCFunction"/>.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the Lua state that the
        ///     function will exist within.</param>
        /// <param name="function">The function from which the Lua function will be created.</param>
        /// <returns>The new Lua function.</returns>
        [SecurityCritical]
        internal static LuaFunction Create( ObjectTranslator objectTranslator, LuaSafeCFunction function )
        {
            using (var lockedMainL = objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 1);

                objectTranslator.PushCFunctionDelegate(L, function);
                LuaFunction luaFunction = new LuaFunction(objectTranslator, L, -1);
                LuaWrapper.lua_pop(L, 1); // function

                return luaFunction;
            }
        }

        internal interface LuaFunctionProxy
        {
            Delegate Delegate
            {
                [SecurityCritical]
                get;
            }

            [SecurityCritical]
            int Call( IntPtr L );
        }

        internal sealed class LuaCFunctionProxy : LuaFunctionProxy
        {
            private readonly ObjectTranslator _objectTranslator;

            [SecurityCritical]
            private readonly LuaCFunction _function;

            [SecuritySafeCritical]
            internal LuaCFunctionProxy( ObjectTranslator objectTranslator, LuaCFunction function )
            {
                this._objectTranslator = objectTranslator;
                this._function = function;
            }

            public Delegate Delegate
            {
                [SecurityCritical]
                get { return _function; }
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
            [SecurityCritical]
            public int Call( IntPtr L )
            {
                try
                {
                    return _function(L);
                }
                catch (SEHException)
                {
                    throw;  // Lua internal; not for us
                }
                catch (Exception ex)
                {
                    return _objectTranslator.Throw(L, ex);
                }
            }
        }

        internal sealed class LuaSafeCFunctionProxy : LuaFunctionProxy
        {
            private readonly ObjectTranslator _objectTranslator;

            [SecurityCritical]
            private readonly LuaSafeCFunction _function;

            [SecuritySafeCritical]
            internal LuaSafeCFunctionProxy( ObjectTranslator objectTranslator, LuaSafeCFunction clrFunction )
            {
                this._objectTranslator = objectTranslator;
                this._function = clrFunction;
            }

            public Delegate Delegate
            {
                [SecurityCritical]
                get { return _function; }
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
            [SecurityCritical]
            public int Call( IntPtr L )
            {
                try
                {
                    var args = new object[LuaWrapper.lua_gettop(L)];

                    for (int i = 0; i < args.Length; ++i)
                        args[i] = _objectTranslator.ToObject(L, i);

                    LuaWrapper.lua_settop(L, 0);

                    var bridge = new LuaThreadBridge(LuaThread.Get(_objectTranslator, L), L, String.Empty);

                    object[] results = _function(bridge, args);

                    ObjectTranslator.CheckStack(L, results.Length);  // results

                    foreach (object result in results)
                        _objectTranslator.PushObject(L, result);

                    return results.Length;
                }
                catch (SEHException)
                {
                    throw;  // Lua internal; not for us
                }
                catch (Exception ex)
                {
                    return _objectTranslator.Throw(L, ex);
                }
            }
        }
    }
}
