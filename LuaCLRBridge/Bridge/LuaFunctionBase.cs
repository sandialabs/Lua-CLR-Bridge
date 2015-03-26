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
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using Lua;

    /// <summary>
    /// Represents a Lua object that can be called like a function.
    /// </summary>
    public class LuaFunctionBase : LuaBase
    {
        [SecurityCritical]
        internal LuaFunctionBase( ObjectTranslator objectTranslator, IntPtr L, int index )
            : base(objectTranslator, L, index)
        {
        }

        /// <summary>
        /// Gets the delegate from which the function was created if the function was created from a delegate.
        /// </summary>
        /// <returns>The delegate from which the function was created if one exists; otherwise, <c>null</c>.
        ///     </returns>
        [SecuritySafeCritical]
        public Delegate AsDelegate()
        {
            using (var lockedL = _objectTranslator.LockedMainState)
            {
                var L = lockedL._L;

                ObjectTranslator.CheckStack(L, 1);

                Push(L); // self
                Delegate @delegate = _objectTranslator.ToCFunctionDelegate(L, -1);
                LuaWrapper.lua_pop(L, 1); // self

                return @delegate;
            }
        }

        /// <summary>
        /// Generates a delegate from the function.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <returns>The delegate.</returns>
        /// <exception cref="ArgumentException">If <typeparamref name="TDelegate"/> is not a delegate type.
        ///     </exception>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Non-generic implementation also provided.")]
        public TDelegate ToDelegate<TDelegate>()
        {
            return (TDelegate)(object)ToDelegate(typeof(TDelegate));
        }

        /// <summary>
        /// Generates a delegate from the function.
        /// </summary>
        /// <param name="delegateType">The delegate type.</param>
        /// <returns>The delegate.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="delegateType"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentException">If <paramref name="delegateType"/> is not a delegate type.
        ///     </exception>
        [SecuritySafeCritical]
        public Delegate ToDelegate( Type delegateType )
        {
            if (delegateType == null)
                throw new ArgumentNullException("delegateType");
            if (!delegateType.IsDelegate())
                throw new ArgumentException("Type must be a delegate type", "delegateType");

            Delegate @delegate;

            if (_objectTranslator.LookupLuaFunctionDelegate(this, delegateType, out @delegate))
                return @delegate;

            MethodInfo signature = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

            ParameterInfo[] parameters = signature.GetParameters();

            // generated delegate variables and body
            var varExprs = new List<ParameterExpression>();
            var bodyExprs = new List<Expression>();

            // generated delegate parameters based on expected signature
            var parameterExprs = new ParameterExpression[parameters.Length];

            // generated delegate return value
            Expression returnExpr;

            // sources of Lua function arguments
            var inExprs = new List<Expression>(parameters.Length);
            var inTypes = new List<Type>(parameters.Length);

            // destinations for Lua function results
            var outExprs = new List<Expression>(1);
            var outTypes = new List<Type>(1);

            if (signature.ReturnType == typeof(void))
            {
                returnExpr = Expression.Default(typeof(void));
            }
            else
            {
                ParameterExpression resultExpr = Expression.Variable(signature.ReturnType, "result");

                varExprs.Add(resultExpr);

                returnExpr = resultExpr;

                outExprs.Add(returnExpr);
                outTypes.Add(signature.ReturnType);
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;

                ParameterExpression parameterExpr = Expression.Parameter(parameterType);

                parameterExprs[i] = parameterExpr;

                if (!parameter.IsOut)
                {
                    inExprs.Add(parameterExpr);
                    inTypes.Add(parameterType.IsByRef ? parameterType.GetElementType() : parameterType);
                }

                if (parameterType.IsByRef)
                {
                    outExprs.Add(parameterExpr);
                    outTypes.Add(parameterType.GetElementType());
                }
            }

            string name = "<>LuaBridge_" + delegateType.Name;  // TODO: use Lua function name if available?

            // Lua function call arguments
            ParameterExpression argsExpr = Expression.Variable(typeof(object[]), "args");

            varExprs.Add(argsExpr);

            Expression initExpr = Expression.Assign(
                argsExpr,
                Expression.NewArrayBounds(typeof(object), Expression.Constant(inExprs.Count, typeof(int))));

            bodyExprs.Add(initExpr);

            // store arguments from sources argument sources
            for (int i = 0; i < inExprs.Count; ++i)
            {
                bodyExprs.Add(Expression.Assign(
                    Expression.ArrayAccess(argsExpr, Expression.Constant(i, typeof(int))),
                    inTypes[i].IsValueType ? Expression.TypeAs(inExprs[i], typeof(object)) : inExprs[i]));
            }

            // Lua function call results
            ParameterExpression retsExpr = Expression.Variable(typeof(object[]), "rets");

            varExprs.Add(retsExpr);

            // call Lua function
            Expression callExpr = Expression.Call(
                Expression.Constant(this, typeof(LuaFunctionBase)),
                typeof(LuaFunctionBase).GetMethod("CallExpectingResults", new[] { typeof(int), typeof(object[]) }),
                Expression.Constant(outExprs.Count, typeof(int)),
                argsExpr);

            bodyExprs.Add(Expression.Assign(
                retsExpr,
                callExpr));

            // LuaBinder instance
            Expression binder = Expression.Constant(LuaBinder.Instance, typeof(LuaBinder));

            // store results back into result destinations, changing types as necessary
            for (int i = 0; i < outExprs.Count; ++i)
            {
                Expression changeTypeCallExpr = Expression.Call(
                    binder,
                    typeof(LuaBinder).GetMethod("ChangeType", new[] { typeof(object), typeof(Type), typeof(CultureInfo) }),
                    Expression.ArrayAccess(retsExpr, Expression.Constant(i, typeof(int))),
                    Expression.Constant(outTypes[i], typeof(Type)),
                    Expression.Constant(null, typeof(CultureInfo)));

                bodyExprs.Add(Expression.Assign(
                    outExprs[i],
                    outTypes[i].IsValueType ? Expression.Unbox(changeTypeCallExpr, outTypes[i]) : Expression.TypeAs(changeTypeCallExpr, outTypes[i])));
            }

            bodyExprs.Add(returnExpr);

            Expression body = Expression.Block(signature.ReturnType, varExprs, bodyExprs);

            @delegate = Expression.Lambda(delegateType, body, name, parameterExprs).Compile();

            _objectTranslator.StoreLuaFunctionDelegate(this, delegateType, ref @delegate);

            return @delegate;

        }

        /// <summary>
        /// Calls the function in the main Lua thread.
        /// </summary>
        /// <param name="args">The arguments to the function.</param>
        /// <returns>The return values from the function.</returns>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the function.
        ///     </exception>
        [SecuritySafeCritical]
        public object[] Call( params object[] args )
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                return Call(_objectTranslator, L, LuaWrapper.LUA_MULTRET, args);
            }
        }

        /// <summary>
        /// Calls the function in the main Lua thread truncating or extending the return values.
        /// </summary>
        /// <param name="resultCount">The number of values returned from the function.</param>
        /// <param name="args">The arguments to the function</param>
        /// <returns>The return values from the function.</returns>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the function.
        ///     </exception>
        [SecuritySafeCritical]
        public object[] CallExpectingResults( int resultCount, params object[] args )
        {
            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                return Call(_objectTranslator, L, resultCount, args);
            }
        }

        /// <summary>
        /// Calls the function in the thread represented by a specified Lua bridge.
        /// </summary>
        /// <param name="bridge">The bridge of the Lua thread.</param>
        /// <param name="args">The arguments to the function.</param>
        /// <returns>The return values from the function.</returns>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the function.
        ///     </exception>
        [SecuritySafeCritical]
        public object[] Call( LuaBridgeBase bridge, params object[] args )
        {
            using (var lockedL = bridge.LockedState)
            {
                var L = lockedL._L;
                var objectTranslator = lockedL._objectTranslator;

                return Call(objectTranslator, L, LuaWrapper.LUA_MULTRET, args);
            }
        }

        /// <summary>
        /// Calls the function in the specified Lua thread truncating or extending the return values.
        /// </summary>
        /// <param name="bridge">The bridge of the Lua thread.</param>
        /// <param name="resultCount">The number of values returned from the function.</param>
        /// <param name="args">The arguments to the function</param>
        /// <returns>The return values from the function.</returns>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the function.
        ///     </exception>
        [SecuritySafeCritical]
        public object[] CallExpectingResults( LuaBridgeBase bridge, int resultCount, params object[] args )
        {
            using (var lockedL = bridge.LockedState)
            {
                var L = lockedL._L;
                var objectTranslator = lockedL._objectTranslator;

                return Call(objectTranslator, L, resultCount, args);
            }
        }

        /// <summary>
        /// Calls the function in a specified Lua thread.
        /// </summary>
        /// <param name="objectTranslator">The object translator for the Lua state that the function exists
        ///     within.</param>
        /// <param name="L">The Lua state.</param>
        /// <param name="retCount">The number of values returned from the function.</param>
        /// <param name="args">The arguments to the function.</param>
        /// <returns>The return values from the function.</returns>
        /// <exception cref="LuaRuntimeException">If there was a Lua error while executing the function.
        ///     </exception>
        [SecurityCritical]
        internal object[] Call( ObjectTranslator objectTranslator, IntPtr L, int retCount, params object[] args )
        {
            ObjectTranslator.CheckStack(L, args.Length + 3);  // stackCollector + self + metamethod + args

            LuaWrapper.lua_pushinteger(L, LuaWrapper.luaW_countlevels(L));
            LuaWrapper.lua_pushcclosure(L, _objectTranslator._stackCollector, 1);

            int top = LuaWrapper.lua_gettop(L);

            Push(L); // self

            if (!LuaWrapper.lua_isfunction(L, -1))
                if (LuaWrapper.luaL_getmetafield(L, -1, "__call", _objectTranslator.Encoding))
                    LuaWrapper.lua_remove(L, -2); // self

            if (retCount != LuaWrapper.LUA_MULTRET && retCount > 0)
                ObjectTranslator.CheckStack(L, retCount - 1);  // -self + rets

            foreach (object arg in args)
                objectTranslator.PushObject(L, arg);

            if (LuaWrapper.lua_pcall(L, args.Length, retCount, top) != LuaStatus.LUA_OK)
            {
                object error = objectTranslator.PopObject(L);

                LuaWrapper.lua_pop(L, 1); // stackCollector

                throw error as Exception ??
                    new LuaRuntimeException(error != null ? error.ToString() : "unspecified error");
            }

            object[] results = PopFunctionCallResults(objectTranslator, L, top);

            LuaWrapper.lua_pop(L, 1); // stackCollector

            return results;
        }

        [SecurityCritical]
        private static object[] PopFunctionCallResults( ObjectTranslator objectTranslator, IntPtr L, int top )
        {
            int nresults = LuaWrapper.lua_gettop(L) - top;
            object[] results = new object[nresults];

            for (int i = nresults - 1; i >= 0; --i)
                results[i] = objectTranslator.PopObject(L);

            return results;
        }
    }
}
