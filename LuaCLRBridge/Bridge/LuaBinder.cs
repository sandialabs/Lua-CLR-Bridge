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
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Lua;

    /// <summary>
    /// Binds members when accessed in Lua. Selects a member from lists of candidates, performs type
    /// inference for generic methods, performs narrowing coercions on numeric values.
    /// </summary>
    internal class LuaBinder : Binder
    {
        public static readonly LuaBinder Instance = new LuaBinder();

        private LuaBinder()
        {
            // nothing to do
        }

        public override FieldInfo BindToField( BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        public MemberInfo BindToFieldOrProperty( BindingFlags bindingAttr, MemberInfo[] match, ref object value, CultureInfo culture )
        {
            BindingFlags allowedAttrs = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;
            Debug.Assert((bindingAttr & ~allowedAttrs) == 0, "LuaBinder accepts only a subset of binding flags.");
            Debug.Assert(culture == null, "LuaBinder is culture-unaware.");

            if (match == null)
                throw new ArgumentNullException("match");
            if (match.Length == 0)
                throw new ArgumentException("Cannot bind field from empty list of candidates", "match");

            MemberInfo candidate = null;
            Type candidateType = null;

            Type valueType = value == null ? null : value.GetType();

            #region Determine possible candidates

            for (int mi = 0; mi < match.Length; ++mi)
            {
                MemberInfo newCandidate = match[mi];
                Type newCandidateType;

                #region Filter by index parameter count

                switch (newCandidate.MemberType)
                {
                    case MemberTypes.Field:
                        newCandidateType = (newCandidate as FieldInfo).FieldType;
                        break;

                    case MemberTypes.Property:
                        PropertyInfo property = newCandidate as PropertyInfo;

                        newCandidateType = property.PropertyType;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                #endregion

                #region Filter by type check

                if ((bindingAttr.HasFlag(BindingFlags.SetField) && newCandidate.MemberType == MemberTypes.Field) ||
                    (bindingAttr.HasFlag(BindingFlags.SetProperty) && newCandidate.MemberType == MemberTypes.Property))
                {
                    if (!CanChangeType(value, valueType, newCandidateType))
                        goto candidateNotApplicable;
                }

                #endregion

                if (candidate != null)
                    throw new AmbiguousMatchException();

                candidate = newCandidate;
                candidateType = newCandidateType;

            candidateNotApplicable:
                ;
            }

            #endregion

            if (candidate == null)
                throw new MissingMemberException();

            if ((bindingAttr.HasFlag(BindingFlags.SetField) && candidate.MemberType == MemberTypes.Field) ||
                (bindingAttr.HasFlag(BindingFlags.SetProperty) && candidate.MemberType == MemberTypes.Property))
            {
                value = ChangeTypeUnsafe(value, valueType, candidateType);
            }

            return candidate;
        }

        public override MethodBase BindToMethod( BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state )
        {
            Debug.Assert(bindingAttr == 0, "LuaBinder accepts only a subset of binding flags.");
            Debug.Assert(modifiers == null, "LuaBinder does not accept parameter modifiers.");
            Debug.Assert(culture == null, "LuaBinder is culture-unaware.");
            Debug.Assert(names == null, "LuaBinder does not support named parameters.");

            if (match == null)
                throw new ArgumentNullException("match");
            if (match.Length == 0)
                throw new ArgumentException("Cannot bind method from empty list of candidates", "match");

            state = null;  // this implementation does not reorder

            List<CandidateMethod> candidates = new List<CandidateMethod>();

            Type[] argTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; ++i)
                if (args[i] != null)
                    argTypes[i] = args[i].GetType();

            #region Determine applicable methods

            for (int mi = 0; mi < match.Length; ++mi)
            {
                var newCandidate = new CandidateMethod();

                newCandidate.Method = match[mi];
                newCandidate.Parameters = newCandidate.Method.GetParameters();

                #region Filter by parameter count

                // nullary method only/always applicable if zero arguments
                if (newCandidate.Parameters.Length == 0)
                {
                    if (args.Length != 0)
                        goto candidateNotApplicable;

                    // cannot do type inference for nullary method
                    if (newCandidate.Method.ContainsGenericParameters)
                        goto candidateNotApplicable;

                    goto determineBetterCandidates;
                }

                ParameterInfo lastParam = newCandidate.Parameters[newCandidate.Parameters.Length - 1];

                // too many args?  (only if not variable-argument)
                if (args.Length > newCandidate.Parameters.Length)
                {
                    if (!lastParam.IsParams())
                        goto candidateNotApplicable;

                    newCandidate.ParamArrayType = lastParam.ParameterType.GetElementType();
                }
                else
                {
                    // too few args?  (only if not enough default parameters; variable-argument param defaults to zero length)
                    for (int pi = args.Length; pi < newCandidate.Parameters.Length - 1; ++pi)
                        if (!newCandidate.Parameters[pi].IsOptional)
                            goto candidateNotApplicable;

                    if (!lastParam.IsOptional)
                    {
                        if (lastParam.IsParams())
                        {
                            newCandidate.ParamArrayType = lastParam.ParameterType.GetElementType();
                        }
                        else if (args.Length < newCandidate.Parameters.Length)
                        {
                            goto candidateNotApplicable;
                        }
                    }
                }

                #endregion

                #region Type inference

                switch (newCandidate.Method.MemberType)
                {
                    case MemberTypes.Constructor:
                        if (newCandidate.Method.ContainsGenericParameters)
                        {
                            /* TODO: If we can identify the constructor after de-genericizing the declaring class,
                                     we can do inference for constructors of generic types. */
                            break;
                        }

                        break;

                    case MemberTypes.Method:
                        if (newCandidate.Method.IsGenericMethodDefinition)
                        {
                            MethodInfo method = newCandidate.Method as MethodInfo;
                            Type[] typeArgs;
                            if (TypeInferer.InferTypeArgs(method.GetGenericArguments(), newCandidate.Parameters, newCandidate.ParamArrayType, args, argTypes, out typeArgs))
                            {
                                newCandidate.Method = method.MakeGenericMethod(typeArgs);
                                newCandidate.Parameters = newCandidate.Method.GetParameters();
                                if (newCandidate.ParamArrayType != null)
                                    newCandidate.ParamArrayType = newCandidate.Parameters[newCandidate.Parameters.Length - 1].ParameterType.GetElementType();
                            }
                        }

                        break;

                    default:
                        throw new InvalidOperationException();
                }

                if (newCandidate.Method.ContainsGenericParameters)
                    goto candidateNotApplicable;

                #endregion

                #region Filter by type check

                for (int ai = 0; ai < args.Length; ++ai)
                {
                    Type paramType = (ai < newCandidate.Parameters.Length - 1 || newCandidate.ParamArrayType == null) ?
                        newCandidate.Parameters[ai].ParameterType :
                        newCandidate.ParamArrayType;

                    if (paramType.IsByRef)
                        paramType = paramType.GetElementType();

                    if (ai == newCandidate.Parameters.Length - 1 && args.Length == newCandidate.Parameters.Length &&
                        newCandidate.ParamArrayType != null &&
                        CanChangeType(args[ai], argTypes[ai], paramType.MakeArrayType()))
                    {
                        // calling variable-argument function in normal form
                        newCandidate.ParamArrayType = null;
                    }
                    else if (!CanChangeType(args[ai], argTypes[ai], paramType))
                    {
                        goto candidateNotApplicable;
                    }
                }

                #endregion

            determineBetterCandidates:
                bool addAsCandidate = true;

                // reverse iterations allows removal
                for (int ci = candidates.Count - 1; ci >= 0; --ci)
                {
                    CandidateMethod candidate = candidates[ci];

                    switch (DetermineBetterFunctionMember(
                        leftMethod: candidate.Method, leftParams: candidate.Parameters, leftParamArrayType: candidate.ParamArrayType,
                        rightMethod: newCandidate.Method, rightParams: newCandidate.Parameters, rightParamArrayType: newCandidate.ParamArrayType,
                        argTypes: argTypes))
                    {
                        case MoreSpecific.Left:
                            addAsCandidate = false;
                            break;
                        case MoreSpecific.Right:
                            candidates.RemoveAt(ci);
                            break;
                        case MoreSpecific.Neither:
                            break;
                    }
                }

                if (addAsCandidate)
                    candidates.Add(newCandidate);

            candidateNotApplicable:
                ;
            }

            #endregion

            switch (candidates.Count)
            {
                case 0:
                    throw new MissingMethodException();

                case 1:
                    CandidateMethod candidate = candidates[0];

                    ChangeArgumentTypes(candidate.Method, candidate.Parameters, candidate.ParamArrayType, ref args, argTypes);

                    return candidate.Method;

                default:
                    throw new AmbiguousMatchException();
            }
        }

        public override object ChangeType( object value, Type type, CultureInfo culture )
        {
            Debug.Assert(culture == null, "LuaBinder is culture-unaware.");

            if (value == null)
            {
                if (!type.IsValueType || type.IsNullable())
                    return value;

                throw new InvalidCastException();
            }

            Type valueType = value.GetType();

            if (type.IsAssignableFrom(valueType))
                return value;

            if (valueType == typeof(double))
            {
                double doubleValue = (double)value;

                if (CanCoerceLuaNumeric(doubleValue, type))
                    return CoerceLuaNumeric(doubleValue, type);
            }

            if (valueType == typeof(LuaFunction) && type.IsDelegate())
            {
                var @delegate = CoerceLuaFunction(value as LuaFunction, type);

                if (@delegate != null)
                    return @delegate;
            }

            throw new InvalidCastException();
        }

        public override void ReorderArgumentArray( ref object[] args, object state )
        {
            throw new NotImplementedException();
        }

        public override MethodBase SelectMethod( BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers )
        {
            throw new NotImplementedException();
        }

        public MethodInfo SelectMethodForDelegate( BindingFlags bindingAttr, MethodInfo[] match, Type delegateType )
        {
            Debug.Assert(bindingAttr == 0, "LuaBinder accepts only a subset of binding flags.");

            if (match == null)
                throw new ArgumentNullException("match");
            if (match.Length == 0)
                throw new ArgumentException("Cannot select method from empty list of candidates", "match");

            if (delegateType == null)
                throw new ArgumentNullException("delegateType");
            if (!delegateType.IsDelegate())
                throw new ArgumentException("Type must be a delegate type", "delegateType");

            List<CandidateMethod> candidates = new List<CandidateMethod>();

            MethodInfo delegateSignature = delegateType.GetMethod("Invoke");

            ParameterInfo[] delegateParameters = delegateSignature.GetParameters();

            var delegateParamTypes = new Type[delegateParameters.Length];
            for (int i = 0; i < delegateParameters.Length; ++i)
                delegateParamTypes[i] = delegateParameters[i].ParameterType;

            var returnType = delegateSignature.ReturnType;

            #region Determine applicable methods

            for (int mi = 0; mi < match.Length; ++mi)
            {
                var newCandidate = new CandidateMethod();

                newCandidate.Method = match[mi];
                newCandidate.Parameters = newCandidate.Method.GetParameters();

                // ECMA-334 15.2

                #region Filter by parameter count

                // method only applicable if exact number of arguments
                if (delegateParamTypes.Length != newCandidate.Parameters.Length)
                    goto candidateNotApplicable;

                #endregion

                #region Type inference

                if (newCandidate.Method.IsGenericMethodDefinition)
                {
                    MethodInfo method = newCandidate.Method as MethodInfo;
                    var args = new object[delegateParamTypes.Length];
                    Type[] typeArgs;
                    if (TypeInferer.InferTypeArgs(method.GetGenericArguments(), newCandidate.Parameters, newCandidate.ParamArrayType, args, delegateParamTypes, out typeArgs))
                    {
                        newCandidate.Method = method.MakeGenericMethod(typeArgs);
                        newCandidate.Parameters = newCandidate.Method.GetParameters();
                        if (newCandidate.ParamArrayType != null)
                            newCandidate.ParamArrayType = newCandidate.Parameters[newCandidate.Parameters.Length - 1].ParameterType.GetElementType();
                    }
                }

                if (newCandidate.Method.ContainsGenericParameters)
                    goto candidateNotApplicable;

                #endregion

                #region Filter by type check

                for (int ai = 0; ai < delegateParamTypes.Length; ++ai)
                {
                    ParameterInfo parameter = newCandidate.Parameters[ai];
                    Type paramType = parameter.ParameterType;

                    if (paramType.IsByRef)
                    {
                        // parameters must have the same modifiers
                        if (parameter.IsOut != delegateParameters[ai].IsOut)
                            goto candidateNotApplicable;

                        // by-reference parameters must have the same types
                        if (paramType != delegateParamTypes[ai])
                            goto candidateNotApplicable;
                    }
                    else
                    {
                        // by-value parameters must have an identity conversion or an implicit reference conversion
                        if (delegateParamTypes[ai].IsValueType)
                        {
                            if (paramType != delegateParamTypes[ai])
                                goto candidateNotApplicable;
                        }
                        else
                        {
                            if (!paramType.IsAssignableFrom(delegateParamTypes[ai]))
                                goto candidateNotApplicable;
                        }
                    }
                }

                var candidateReturnType = (newCandidate.Method as MethodInfo).ReturnType;

                // return type must have an identity conversion or implicit reference conversion
                if (candidateReturnType.IsValueType)
                {
                    if (returnType != candidateReturnType)
                        goto candidateNotApplicable;
                }
                else
                {
                    if (!returnType.IsAssignableFrom(candidateReturnType))
                        goto candidateNotApplicable;
                }

                #endregion

                bool addAsCandidate = true;

                // reverse iterations allows removal
                for (int ci = candidates.Count - 1; ci >= 0; --ci)
                {
                    CandidateMethod candidate = candidates[ci];

                    switch (DetermineBetterFunctionMember(
                        leftMethod: candidate.Method, leftParams: candidate.Parameters, leftParamArrayType: candidate.ParamArrayType,
                        rightMethod: newCandidate.Method, rightParams: newCandidate.Parameters, rightParamArrayType: newCandidate.ParamArrayType,
                        argTypes: delegateParamTypes))
                    {
                        case MoreSpecific.Left:
                            addAsCandidate = false;
                            break;
                        case MoreSpecific.Right:
                            candidates.RemoveAt(ci);
                            break;
                        case MoreSpecific.Neither:
                            break;
                    }
                }

                if (addAsCandidate)
                    candidates.Add(newCandidate);

            candidateNotApplicable:
                ;
            }

            #endregion

            switch (candidates.Count)
            {
                case 0:
                    throw new MissingMethodException();

                case 1:
                    CandidateMethod candidate = candidates[0];

                    return candidate.Method as MethodInfo;

                default:
                    throw new AmbiguousMatchException();
            }
        }

        public override PropertyInfo SelectProperty( BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers )
        {
            throw new NotImplementedException();
        }

        internal static MemberInfo[] RemoveHidden( IEnumerable<MemberInfo> members )
        {
            Type mostDerivedNonMethodBaseDeclaringType = typeof(object);
            Type mostDerivedMethodBaseDeclaringType = typeof(object);

            // all non-MethodBase members from most derived type
            List<MemberInfo> nonMethodBases = new List<MemberInfo>();

            // all unhidden MethodBase members from inheritance hierarchy
            List<MethodBase> methodBases = new List<MethodBase>();

            foreach (var member in members)
            {
                Type declaringType = member.DeclaringType;

                bool add = true;

                switch (member.MemberType)
                {
                    case MemberTypes.Constructor:
                    case MemberTypes.Method:
                        var method = member as MethodBase;

                        if (declaringType.IsSubclassOf(mostDerivedNonMethodBaseDeclaringType))
                        {
                            // hide non-MethodBase members in base classes
                            nonMethodBases.Clear();
                        }
                        else if (declaringType != mostDerivedNonMethodBaseDeclaringType)
                        {
                            Debug.Assert(mostDerivedNonMethodBaseDeclaringType.IsAssignableFrom(declaringType));

                            // current member is hidden by a non-MethodBase member in a derived class
                            add = false;
                        }

                        if (declaringType.IsSubclassOf(mostDerivedMethodBaseDeclaringType))
                        {
                            /* for efficiency, by-signature method hiding happens during overload resolution */

                            if (!method.IsHideBySig)
                                methodBases.Clear();

                            mostDerivedMethodBaseDeclaringType = declaringType;
                        }
                        else
                        {
                            /* for efficiency, by-signature method hiding happens during overload resolution */

                            if (!method.IsHideBySig)
                                methodBases.RemoveAll(( methodBase ) => declaringType.IsSubclassOf(methodBase.DeclaringType));

                            if (add && methodBases.Any(( methodBase ) => !methodBase.IsHideBySig && methodBase.DeclaringType.IsSubclassOf(declaringType)))
                            {
                                // current member is hidden by a hide-by-name MethodBase in a derived class
                                add = false;
                            }
                        }

                        if (add)
                            methodBases.Add(method);
                        break;

                    case MemberTypes.Custom:
                    case MemberTypes.TypeInfo:
                        // ignore mystery members
                        break;

                    case MemberTypes.Event:
                    case MemberTypes.Field:
                    case MemberTypes.NestedType:
                    case MemberTypes.Property:
                        if (declaringType.IsSubclassOf(mostDerivedNonMethodBaseDeclaringType))
                        {
                            // hide non-MethodBase members in base classes
                            nonMethodBases.Clear();

                            mostDerivedNonMethodBaseDeclaringType = declaringType;
                        }
                        else if (declaringType != mostDerivedNonMethodBaseDeclaringType)
                        {
                            // current member is hidden by a non-MethodBase member in a derived class
                            add = false;
                        }

                        if (declaringType.IsSubclassOf(mostDerivedMethodBaseDeclaringType))
                        {
                            // hide MethodBase members in base classes
                            methodBases.Clear();
                        }
                        else
                        {
                            // hide MethodBase members in base classes
                            methodBases.RemoveAll(( methodBase ) => declaringType.IsSubclassOf(methodBase.DeclaringType));

                            if (add && declaringType != mostDerivedMethodBaseDeclaringType)
                            {
                                // current member is hidden by a MethodBase member in a derived class
                                add = false;
                            }
                        }

                        if (add)
                            nonMethodBases.Add(member);
                        break;

                    default:
                        throw new InvalidOperationException("Should never happen!");
                }
            }

            nonMethodBases.AddRange(methodBases);
            return nonMethodBases.ToArray();
        }

        // assumes prior type checking
        private static object ChangeTypeUnsafe( object value, Type valueType, Type targetType )
        {
            if (value == null)
                return value;

#if false  // this is the assumed case if nothing else matches
            if (targetType == typeof(object) ||  // fast pass
                targetType.IsAssignableFrom(valueType))
            {
                return value;
            }
#endif

            if (valueType == typeof(double))
                return CoerceLuaNumeric((double)value, targetType);

            if (valueType == typeof(LuaFunction) && targetType.IsDelegate())
                return CoerceLuaFunction(value as LuaFunction, targetType);

            return value;
        }

        private static void ChangeArgumentTypes( MethodBase method, ParameterInfo[] parameters, Type paramArrayType, ref object[] args, Type[] argTypes )
        {
            if (parameters.Length == 0)
                return;

            object[] changedArgs = new object[parameters.Length];

            if (args.Length == parameters.Length)
            {
                int ai = 0;

                for (; ai < parameters.Length - 1; ++ai)
                    changedArgs[ai] = ChangeArgumentType(parameters[ai], args[ai], argTypes[ai]);

                if (paramArrayType == null)
                    changedArgs[ai] = ChangeArgumentType(parameters[ai], args[ai], argTypes[ai]);
                else
                {
                    Array paramsArray = Array.CreateInstance(paramArrayType, 1);
                    changedArgs[ai] = paramsArray;

                    paramsArray.SetValue(ChangeTypeUnsafe(args[ai], argTypes[ai], paramArrayType), 0);
                }
            }
            else if (args.Length < parameters.Length)
            {
                int ai = 0;

                for (; ai < args.Length; ++ai)
                    changedArgs[ai] = ChangeArgumentType(parameters[ai], args[ai], argTypes[ai]);

                for (; ai < parameters.Length - 1; ++ai)
                    changedArgs[ai] = parameters[ai].DefaultValue;

                if (paramArrayType == null)
                    changedArgs[ai] = parameters[ai].DefaultValue;
                else
                {
                    // default value for parameter-array parameter is empty array
                    changedArgs[ai] = Array.CreateInstance(paramArrayType, 0);
                }
            }
            else // (args.Length > @params.Length)
            {
                int ai = 0;

                for (; ai < parameters.Length - 1; ++ai)
                    changedArgs[ai] = ChangeArgumentType(parameters[ai], args[ai], argTypes[ai]);

                Debug.Assert(paramArrayType != null, "Cannot have null parameter array type if there are more arguments than parameters.");

                Array paramsArray = Array.CreateInstance(paramArrayType, args.Length - parameters.Length + 1);
                changedArgs[ai] = paramsArray;

                for (int pai = 0; ai < @args.Length; ++ai, ++pai)
                    paramsArray.SetValue(ChangeTypeUnsafe(args[ai], argTypes[ai], paramArrayType), pai);
            }

            args = changedArgs;
        }

        private static object ChangeArgumentType( ParameterInfo param, object arg, Type argType )
        {
            Type paramType = param.ParameterType;
            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

            return ChangeTypeUnsafe(arg, argType, paramType);
        }

        private static bool CanChangeType( object value, Type valueType, Type targetType )
        {
            /* TODO: implement as per ECMA-334 6.1 */

            if (value == null)
            {
                // non-nullable value type cannot be assigned null
                return !targetType.IsValueType || targetType.IsNullable();
            }

            if (targetType == typeof(object) ||  // fast pass
                targetType.IsAssignableFrom(valueType))
            {
                return true;
            }

            if (valueType == typeof(double))
                return CanCoerceLuaNumeric((double)value, targetType);

            if (valueType == typeof(LuaFunction) && targetType.IsDelegate())
                return CanCoerceLuaFunction(value as LuaFunction, targetType);

            return false;
        }

        private static bool CanCoerceLuaFunction( LuaFunction value, Type targetType )
        {
            if (targetType == typeof(LuaCFunction) ||
                targetType == typeof(LuaSafeCFunction))
            {
                Delegate delegateValue = value.AsDelegate();

                return delegateValue != null && delegateValue.GetType() == targetType;
            }

            return true;
        }

        private static Delegate CoerceLuaFunction( LuaFunction value, Type targetType )
        {
            if (targetType == typeof(LuaCFunction) ||
                targetType == typeof(LuaSafeCFunction))
            {
                Delegate delegateValue = value.AsDelegate();

                if (delegateValue != null && delegateValue.GetType() == targetType)
                    return delegateValue;
            }
            else
            {
                return value.ToDelegate(targetType);
            }

            return null;
        }

        // deviation from C# implicit conversion
        private static bool CanCoerceLuaNumeric( double value, Type targetType )
        {
            if (!targetType.IsPrimitive && targetType.IsNullable())
                targetType = Nullable.GetUnderlyingType(targetType);

            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Double:
                    return true;
                case TypeCode.Single:
                    return Math.Abs(value) <= Single.MaxValue || Double.IsInfinity(value) || Double.IsNaN(value);

                case TypeCode.Int64:
                    return value >= Int64.MinValue && value <= Int64.MaxValue && value % 1 == 0;
                case TypeCode.UInt64:
                    return value >= UInt64.MinValue && value <= UInt64.MaxValue && value % 1 == 0;
                case TypeCode.Int32:
                    return value >= Int32.MinValue && value <= Int32.MaxValue && value % 1 == 0;
                case TypeCode.UInt32:
                    return value >= UInt32.MinValue && value <= UInt32.MaxValue && value % 1 == 0;
                case TypeCode.Int16:
                    return value >= Int16.MinValue && value <= Int16.MaxValue && value % 1 == 0;
                case TypeCode.UInt16:
                    return value >= UInt16.MinValue && value <= UInt16.MaxValue && value % 1 == 0;
                case TypeCode.SByte:
                    return value >= SByte.MinValue && value <= SByte.MaxValue && value % 1 == 0;
                case TypeCode.Byte:
                    return value >= Byte.MinValue && value <= Byte.MaxValue && value % 1 == 0;

                case TypeCode.Char:
                    return value >= Char.MinValue && value <= Char.MaxValue && value % 1 == 0;

                default:
                    return false;
            }
        }

        // deviation from C# implicit conversion
        private static object CoerceLuaNumeric( double value, Type targetType )
        {
            if (!targetType.IsPrimitive && targetType.IsNullable())
                targetType = Nullable.GetUnderlyingType(targetType);

            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Double:
                    return value;
                case TypeCode.Single:
                    return (Single)value;

                case TypeCode.Int64:
                    return (Int64)value;
                case TypeCode.UInt64:
                    return (UInt64)value;
                case TypeCode.Int32:
                    return (Int32)value;
                case TypeCode.UInt32:
                    return (UInt32)value;
                case TypeCode.Int16:
                    return (Int16)value;
                case TypeCode.UInt16:
                    return (UInt16)value;
                case TypeCode.SByte:
                    return (SByte)value;
                case TypeCode.Byte:
                    return (Byte)value;

                case TypeCode.Char:
                    return (Char)value;

                default:  // IComparable, etc.
                    return value;
            }
        }

        // ECMA 6.1
        private static bool HasImplicitConversion( Type type, Type targetType )
        {
            #region 6.1.1 identity conversion

            if (type == targetType)
                return true;

            #endregion

            #region 6.1.2 implicit numeric conversion

            if (type.IsPrimitive && targetType.IsPrimitive)
            {
                TypeCode targetTypeCode = Type.GetTypeCode(targetType);

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                        if (targetTypeCode == TypeCode.Int16)
                            return true;
                        goto case TypeCode.Int16;
                    case TypeCode.Byte:
                        if (targetTypeCode == TypeCode.Int16 || targetTypeCode == TypeCode.UInt16)
                            return true;
                        goto case TypeCode.UInt16;
                    case TypeCode.Int16:
                        if (targetTypeCode == TypeCode.Int32)
                            return true;
                        goto case TypeCode.Int32;
                    case TypeCode.UInt16:
                        if (targetTypeCode == TypeCode.Int32 || targetTypeCode == TypeCode.UInt32)
                            return true;
                        goto case TypeCode.UInt32;
                    case TypeCode.Int32:
                        if (targetTypeCode == TypeCode.Int64)
                            return true;
                        goto case TypeCode.Int64;
                    case TypeCode.UInt32:
                        if (targetTypeCode == TypeCode.Int64 || targetTypeCode == TypeCode.UInt64)
                            return true;
                        goto case TypeCode.UInt64;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        if (targetTypeCode == TypeCode.Single || targetTypeCode == TypeCode.Double || targetTypeCode == TypeCode.Decimal)
                            return true;
                        break;

                    case TypeCode.Char:
                        if (targetTypeCode == TypeCode.UInt16)
                            return true;
                        goto case TypeCode.UInt16;

                    case TypeCode.Single:
                        if (targetTypeCode == TypeCode.Double)
                            return true;
                        break;
                }
            }

            #endregion

            // 6.1.3 implicit enumeration conversion
            //// not implemented because this implementation does not operate on expressions

            #region 6.1.4 implicit nullable conversion

            Type targetTypeNullable = Nullable.GetUnderlyingType(targetType);

            if (targetTypeNullable != null)
            {
                if (HasImplicitConversion(type, targetTypeNullable))
                    return true;

                Type typeNullable = Nullable.GetUnderlyingType(type);

                if (typeNullable != null)
                    if (HasImplicitConversion(typeNullable, targetTypeNullable))
                        return true;
            }

            #endregion

            // 6.1.5 null literal conversion
            //// not implemented because this implementation does not operate on expressions

            #region 6.1.6 implicit reference conversions, 6.1.7 boxing conversions

            // .6: reference-type to object
            // .7: value-type to object
            if (targetType == typeof(object))
                return true;

            // .6: class-type to base class-type
            // .6: class-type to implemented interface-type
            // .6: interface-type to base interface-type
            // .7: non-nullable-value-type to ValueType
            // .7: non-nullable-value-type to implemented interface-type
            // .7: nullable-value-type to reference-type where non-nullable-value-type has boxing conversion
            // .7: enum-type to Enum
            // .6: array-type to same-dimensionality array-type where element types are reference-type have implicit reference conversion
            // .6: single-dimension array to IList<> (and its bases) where element type has implicit reference conversion
            if (targetType.IsAssignableFrom(type))
                return true;

            /* TODO: variance-convertible? */

            #endregion

            // 6.1.8 implicit dynamic conversions
            //// not implemented because dynamic is not a type

            // 6.1.9 implicit constant expression conversions
            //// not implemented because this implementation does not operate on expressions

            // 6.1.10 implicit conversions involving type parameters
            //// TODO?

            // 6.1.11 user-defined implicit conversions
            //// TODO

            // 6.1.12 anonymous function conversions and method group conversions
            //// not implemented because this implementation does not operate on expressions

            return false;
        }

        private enum MoreSpecific
        {
            Left,
            Right,
            Neither,
        }

        // ECMA-334 7.5.3.2
        private static MoreSpecific DetermineBetterFunctionMember( MethodBase leftMethod, ParameterInfo[] leftParams, Type leftParamArrayType,
                                                                   MethodBase rightMethod, ParameterInfo[] rightParams, Type rightParamArrayType, Type[] argTypes )
        {
            int leftParamCount = leftParams.Length;
            int rightParamCount = rightParams.Length;

            /* overriding/hiding -- this is not actually part of determining the better function member,
                                    but happens here for convenience and efficiency */

            if (leftParamCount == rightParamCount &&
                leftParamArrayType == rightParamArrayType &&
                HaveEqualTypes(leftParams, rightParams))
            {
                /* more-derived is better */

                Type leftDeclaringType = leftMethod.DeclaringType;
                Type rightDeclaringType = rightMethod.DeclaringType;

                if (leftDeclaringType.IsSubclassOf(rightDeclaringType))
                    return MoreSpecific.Left;
                else if (rightDeclaringType.IsSubclassOf(leftDeclaringType))
                    return MoreSpecific.Right;
            }

            /* method with some-better-and-no-worse implicit conversions from arg types to param types is better */

            bool identicalExpandedParamTypes = true;

            MoreSpecific betterMethod = MoreSpecific.Neither;

            for (int ai = 0; ai < argTypes.Length; ++ai)
            {
                Type leftParamType = (ai < leftParamCount - 1 || leftParamArrayType == null) ?
                    leftParams[ai].ParameterType :
                    leftParamArrayType;

                if (leftParamType.IsByRef)
                    leftParamType = leftParamType.GetElementType();

                Type rightParamType = (ai < rightParamCount - 1 || rightParamArrayType == null) ?
                    rightParams[ai].ParameterType :
                    rightParamArrayType;

                if (rightParamType.IsByRef)
                    rightParamType = rightParamType.GetElementType();

                identicalExpandedParamTypes = identicalExpandedParamTypes && leftParamType == rightParamType;

                MoreSpecific betterConversion = DetermineBetterConversionFromType(type: argTypes[ai], leftType: leftParamType, rightType: rightParamType);

                if (betterMethod != betterConversion && betterConversion != MoreSpecific.Neither)
                {
                    if (betterMethod != MoreSpecific.Neither)
                    {
                        Debug.Assert(!identicalExpandedParamTypes, "Cannot have one method be better than another if they have identical expanded parameters.");

                        betterMethod = MoreSpecific.Neither;
                        break;
                    }

                    betterMethod = betterConversion;
                }
            }

            if (betterMethod != MoreSpecific.Neither)
                return betterMethod;

            /* tie breakers only if parameters all are identical */

            if (!identicalExpandedParamTypes)
                return MoreSpecific.Neither;

            if (leftMethod.IsGenericMethod || rightMethod.IsGenericMethod)
            {
                /* non-generic method is better */

                if (!leftMethod.IsGenericMethod)
                    return MoreSpecific.Left;
                else if (!rightMethod.IsGenericMethod)
                    return MoreSpecific.Right;
            }

            if (leftParamArrayType == null || rightParamArrayType == null)
            {
                /* method applicable in normal/non-expanded form is better */

                if (rightParamArrayType != null)
                    return MoreSpecific.Left;
                else if (leftParamArrayType != null)
                    return MoreSpecific.Right;

                /* method with exact number of arguments is better */

                if (leftParamCount != rightParamCount)
                {
                    if (leftParamCount == argTypes.Length)
                        return MoreSpecific.Left;
                    else if (rightParamCount == argTypes.Length)
                        return MoreSpecific.Right;
                }
            }
            else  // both expanded-form
            {
                /* method with more declared parameters is better between two expanded-form */

                if (leftParamCount > rightParamCount)
                    return MoreSpecific.Left;
                else if (rightParamCount > leftParamCount)
                    return MoreSpecific.Right;
            }

            /* method with more-specific parameter types is better */

            ParameterInfo[] genericLeftMethodParams =
                leftMethod.MemberType == MemberTypes.Method && leftMethod.IsGenericMethod ?
                (leftMethod as MethodInfo).GetGenericMethodDefinition().GetParameters() :
                leftParams;
            ParameterInfo[] genericRightMethodParams =
                rightMethod.MemberType == MemberTypes.Method && rightMethod.IsGenericMethod ?
                (rightMethod as MethodInfo).GetGenericMethodDefinition().GetParameters() :
                rightParams;

            if (leftParamCount == rightParamCount)
            {
                for (int pi = 0; pi < leftParamCount; ++pi)
                {
                    Debug.Assert(leftParams[pi].ParameterType == rightParams[pi].ParameterType, "Cannot have different parameters if both methods are in the same form and expanded parameters were identical.");

                    MoreSpecific moreSpecificParamType = DetermineMoreSpecificParameterType(
                        type: leftParams[pi].ParameterType,
                        genericLeftType: genericLeftMethodParams[pi].ParameterType,
                        genericRightType: genericRightMethodParams[pi].ParameterType);

                    if (betterMethod != moreSpecificParamType && moreSpecificParamType != MoreSpecific.Neither)
                    {
                        if (betterMethod != MoreSpecific.Neither)
                        {
                            betterMethod = MoreSpecific.Neither;
                            break;
                        }

                        betterMethod = moreSpecificParamType;
                    }
                }

                if (betterMethod != MoreSpecific.Neither)
                    return betterMethod;
            }

            /* TODO: non-lifted operator is better */

            return MoreSpecific.Neither;
        }

        private static bool HaveEqualTypes( ParameterInfo[] left, ParameterInfo[] right )
        {
            Debug.Assert(left.Length == right.Length, "Cannot have different lengths.");

            for (int i = 0; i < left.Length; ++i)
                if (left[i].ParameterType != right[i].ParameterType)
                    return false;

            return true;
        }

        // part of ECMA-334 7.5.3.2
        private static MoreSpecific DetermineMoreSpecificParameterType( Type type, Type genericLeftType, Type genericRightType )
        {
            /* non-generic is more specific */

            if (genericLeftType.IsGenericParameter || genericRightType.IsGenericParameter)
            {
                if (!genericLeftType.IsGenericParameter)
                    return MoreSpecific.Left;
                else if (!genericRightType.IsGenericParameter)
                    return MoreSpecific.Right;
            }

            /* types are checked recursively */

            if (type.IsArray)
            {
                Debug.Assert(genericLeftType.IsArray, "Generic definition of left type must have same arrayness as non-generic type.");
                Debug.Assert(genericRightType.IsArray, "Generic definition of right type must have same arrayness as non-generic type.");

                return DetermineMoreSpecificParameterType(
                    type: type.GetElementType(),
                    genericLeftType: genericLeftType.GetElementType(),
                    genericRightType: genericRightType.GetElementType());
            }
            else if (type.IsGenericType)
            {
                Debug.Assert(genericLeftType.IsGenericType, "Generic definition of left type must have same genericness as non-generic type.");
                Debug.Assert(genericRightType.IsGenericType, "Generic definition of right type must have same genericness as non-generic type.");

                Type[] typeArgs = type.GetGenericArguments();
                Type[] genericLeftTypeArgs = genericLeftType.GetGenericArguments();
                Type[] genericRightTypeArgs = genericRightType.GetGenericArguments();

                Debug.Assert(genericLeftTypeArgs.Length == typeArgs.Length, "Generic definition of left type must have same type-argument arity as non-generic type.");
                Debug.Assert(genericRightTypeArgs.Length == typeArgs.Length, "Generic definition of right type must have same type-argument arity as non-generic type.");

                MoreSpecific moreSpecificType = MoreSpecific.Neither;

                for (int i = 0; i < genericLeftTypeArgs.Length; ++i)
                {
                    MoreSpecific moreSpecificTypeArg = DetermineMoreSpecificParameterType(
                        type: typeArgs[i],
                        genericLeftType: genericLeftTypeArgs[i],
                        genericRightType: genericRightTypeArgs[i]);

                    if (moreSpecificType != moreSpecificTypeArg && moreSpecificTypeArg != MoreSpecific.Neither)
                    {
                        if (moreSpecificType != MoreSpecific.Neither)
                        {
                            moreSpecificType = MoreSpecific.Neither;
                            break;
                        }

                        moreSpecificType = moreSpecificTypeArg;
                    }
                }

                if (moreSpecificType != MoreSpecific.Neither)
                    return moreSpecificType;
            }

            return MoreSpecific.Neither;
        }

        // ECMA-334 7.5.3.3, 7.5.3.4, 7.5.3.5
        private static MoreSpecific DetermineBetterConversionFromType( Type type, Type leftType, Type rightType )
        {
            /* "better conversion from expression" is implemented as "better conversion from type"
               because this implementation does not operate on expressions */

            if (leftType == rightType)
                return MoreSpecific.Neither;

            /* type with identity conversion is better */

            if (leftType == type)
                return MoreSpecific.Left;
            else if (rightType == type)
                return MoreSpecific.Right;

            /* signed integral type is better than same-width-or-wider unsigned integral type */

            if (leftType.IsPrimitive && rightType.IsPrimitive)
            {
                TypeCode leftTypeCode = Type.GetTypeCode(leftType);
                TypeCode rightTypeCode = Type.GetTypeCode(rightType);

                if (IsBetterIntegerConversionTarget(typeCode: leftTypeCode, otherTypeCode: rightTypeCode))
                    return MoreSpecific.Left;
                else if (IsBetterIntegerConversionTarget(typeCode: rightTypeCode, otherTypeCode: leftTypeCode))
                    return MoreSpecific.Right;
            }

            /* type with implicit conversion to other is better */

            bool leftToRight = HasImplicitConversion(type: leftType, targetType: rightType);
            bool rightToLeft = HasImplicitConversion(type: rightType, targetType: leftType);

            if (leftToRight == rightToLeft)
            {
                return MoreSpecific.Neither;
            }
            else if (leftToRight)
            {
                return MoreSpecific.Left;
            }
            else  // rightToLeft
            {
                return MoreSpecific.Right;
            }
        }

        // part of ECMA-334 7.5.3.5
        private static bool IsBetterIntegerConversionTarget( TypeCode typeCode, TypeCode otherTypeCode )
        {
            switch (typeCode)
            {
                case TypeCode.SByte:
                    if (otherTypeCode == TypeCode.Byte)
                        return true;
                    goto case TypeCode.Int16;
                case TypeCode.Int16:
                    if (otherTypeCode == TypeCode.UInt16)
                        return true;
                    goto case TypeCode.Int32;
                case TypeCode.Int32:
                    if (otherTypeCode == TypeCode.UInt32)
                        return true;
                    goto case TypeCode.Int64;
                case TypeCode.Int64:
                    if (otherTypeCode == TypeCode.UInt64)
                        return true;
                    break;
            }

            return false;
        }

        private struct CandidateMethod
        {
            public MethodBase Method;
            public ParameterInfo[] Parameters;
            public Type ParamArrayType;
        }

        private static class TypeInferer
        {
            private enum Bound
            {
                Lower,
                Upper,
                Exact,
                /* TODO: Lua only has one number type, but maybe we could infer the type bounds of the actual numeric value, or
                         potentially we could infer the type to be "Numeric" and replace that with every numeric type after inference, or
                         maybe we can just replace all inferred "double"s with every numeric type. */
                //// Numeric,
            }

            public static bool InferTypeArgs( Type[] genericTypeArgs, ParameterInfo[] @params, Type paramArrayType, object[] args, Type[] argTypes, out Type[] typeArgs )
            {
                List<Tuple<Bound, Type>>[] typeArgBounds = new List<Tuple<Bound, Type>>[genericTypeArgs.Length];

                int lastParamIndex = @params.Length - 1;

                bool hasNullArg = false;

                for (int i = 0; i < argTypes.Length; ++i)
                {
                    ParameterInfo param = @params[Math.Min(i, lastParamIndex)];

                    Type argType = argTypes[i];

                    if (argType == null)
                    {
                        hasNullArg = true;
                        continue;
                    }

                    Type paramType =
                        (i >= lastParamIndex && paramArrayType != null && !(argTypes.Length == @params.Length && argType.IsArray)) ?  // calling variable-argument in extended form
                        paramArrayType :
                        param.ParameterType;

                    Bound bound =
                        !paramType.IsByRef ? Bound.Lower :
                        param.IsOut ? Bound.Upper :
                        Bound.Exact;

                    if (!InferTypeArgBound(bound, typeArgBounds, paramType, args[i], argType))
                    {
                        typeArgs = null;
                        return false;
                    }
                }

                // divergence from C# type inference!
                // because nulls are untyped in Lua, to be able to call generic methods using null arguments, we
                // have to infer some bounds for otherwise unbound type arguments that were passed null arguments
                if (hasNullArg)
                {
                    for (int ta = 0; ta < typeArgBounds.Length; ++ta)
                    {
                        if (typeArgBounds[ta] == null)
                        {
                            for (int a = 0; a < argTypes.Length; ++a)
                            {
                                if (argTypes[a] == null)
                                {
                                    AddTypeArgBound(ref typeArgBounds[ta], Tuple.Create(Bound.Exact, typeof(object)));
                                    break;
                                }
                            }
                        }
                    }
                }

                return CaculateTypeArgsFromBounds(typeArgBounds, out typeArgs);
            }

            private static bool InferTypeArgBound( Bound bound, List<Tuple<Bound, Type>>[] bounds, Type paramType, object arg, Type argType )
            {
                if (paramType.IsByRef)
                    paramType = paramType.GetElementType();

                if (paramType.IsGenericParameter)
                {
                    AddTypeArgBound(ref bounds[paramType.GenericParameterPosition], Tuple.Create(bound, argType));
                }
                else if (paramType.IsArray)
                {
                    if (!argType.IsArray || paramType.GetArrayRank() != argType.GetArrayRank())
                        return false;

                    return InferTypeArgBound(bound, bounds, paramType.GetElementType(), null, argType.GetElementType());
                }
                else if (paramType.IsGenericType)
                {
                    Type[] paramTypeArgs = paramType.GetGenericArguments();
                    Type[] argTypeArgs = argType.GetGenericArguments();

                    if (paramTypeArgs.Length != argTypeArgs.Length)
                        return false;

                    Type genericParamType = paramType.GetGenericTypeDefinition();
                    Type[] genericParamTypeArgs = genericParamType.GetGenericArguments();

                    for (int i = 0; i < paramTypeArgs.Length; ++i)
                    {
                        Type paramTypeArg = paramTypeArgs[i];

                        Type genericParamTypeArg = genericParamTypeArgs[i];
                        GenericParameterAttributes genericParamTypeArgAttrs = genericParamTypeArg.GenericParameterAttributes;

                        Bound genericParamTypeArgBound =
                            genericParamTypeArgAttrs.HasFlag(GenericParameterAttributes.Contravariant) ? Bound.Upper :  // in
                            genericParamTypeArgAttrs.HasFlag(GenericParameterAttributes.Covariant) ? Bound.Lower :  // out
                            Bound.Exact;

                        if (!InferTypeArgBound(genericParamTypeArgBound, bounds, paramTypeArg, null, argTypeArgs[i]))
                            return false;
                    }
                }

                return true;
            }

            private static void AddTypeArgBound( ref List<Tuple<Bound, Type>> bounds, Tuple<Bound, Type> bound )
            {
                if (bounds == null)
                {
                    bounds = new List<Tuple<Bound, Type>>();
                    bounds.Add(bound);
                    return;
                }

                if (!bounds.Contains(bound))
                    bounds.Add(bound);
            }

            private static bool CaculateTypeArgsFromBounds( List<Tuple<Bound, Type>>[] bounds, out Type[] typeArgs )
            {
                typeArgs = new Type[bounds.Length];

                for (int i = 0; i < typeArgs.Length; ++i)
                {
                    Type upperBound = null;
                    Type lowerBound = null;

                    if (bounds[i] != null)
                    {
                        foreach (var bound in bounds[i])
                        {
                            switch (bound.Item1)
                            {
                                case Bound.Lower:
                                    Type candidateLowerBound = bound.Item2;

                                    if (upperBound != null && !upperBound.IsAssignableFrom(candidateLowerBound))
                                        return false;  // unsatisfiable bounds

                                    if (lowerBound == null || candidateLowerBound.IsAssignableFrom(lowerBound))
                                        lowerBound = candidateLowerBound;
                                    else if (!candidateLowerBound.IsSubclassOf(lowerBound))
                                        return false;  // unrelated lower bounds (according to C# type inference)
                                    break;

                                case Bound.Upper:
                                    Type candidateUpperBound = bound.Item2;

                                    if (lowerBound != null && !candidateUpperBound.IsAssignableFrom(lowerBound))
                                        return false;  // unsatisfiable bounds

                                    if (upperBound == null || upperBound.IsAssignableFrom(candidateUpperBound))
                                        upperBound = candidateUpperBound;
                                    else if (!upperBound.IsSubclassOf(candidateUpperBound))
                                        return false;  // unrelated upper bounds
                                    break;

                                case Bound.Exact:
                                    Type candidateExactBound = bound.Item2;

                                    if (upperBound != null && !upperBound.IsAssignableFrom(candidateExactBound))
                                        return false;  // unsatisfiable bounds

                                    if (lowerBound != null && !candidateExactBound.IsAssignableFrom(lowerBound))
                                        return false;  // unsatisfiable bounds

                                    upperBound = candidateExactBound;
                                    lowerBound = candidateExactBound;
                                    break;
                            }
                        }
                    }

                    typeArgs[i] = upperBound ?? lowerBound;

                    if (typeArgs[i] == null)
                        return false;  // unbound type argument
                }

                return true;
            }
        }
    }
}
