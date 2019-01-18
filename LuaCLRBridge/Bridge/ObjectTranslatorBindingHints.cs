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
    using System.Reflection;

    internal partial class ObjectTranslator
    {
        /// <summary>
        /// Provides hints for resolving members.
        /// </summary>
        private class MemberBindingHints
        {
            /// <summary>
            /// The member-binding hints to be used when no hints are specified.
            /// </summary>
            internal static readonly MemberBindingHints DefaultHints = new MemberBindingHints();

            private const MemberTypes _allGetMemberTypes =
                MemberTypes.Event |
                MemberTypes.Field |
                MemberTypes.Method |
                MemberTypes.NestedType |
                MemberTypes.Property;

            private const MemberTypes _allSetMemberTypes =
                MemberTypes.Field |
                MemberTypes.Property;

#pragma warning disable 0649
            private MemberTypes _memberTypes;  // TODO: to be set by a binding hint in Lua
#pragma warning restore 0649

            private MethodAttributes _methodAttributesMask = MethodAttributes.SpecialName;
            private MethodAttributes _methodAttributes;

            private EventAttributes _eventAttributesMask = EventAttributes.SpecialName;
            private EventAttributes _eventAttributes;

            private FieldAttributes _fieldAttributesMask = FieldAttributes.SpecialName;
            private FieldAttributes _fieldAttributes;

            private TypeAttributes _typeAttributesMask = TypeAttributes.SpecialName;
            private TypeAttributes _typeAttributes;

            private PropertyAttributes _propertyAttributesMask = PropertyAttributes.SpecialName;
            private PropertyAttributes _propertyAttributes;

            private MemberBindingHints()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MemberBindingHints"/> class from the entries in the
            /// specified Lua table.
            /// </summary>
            /// <param name="hintTable">The Lua table containing binding hints.</param>
            /// <param name="target">The target of the hints; used for specializing generic type definitions.</param>
            /// <exception cref="BindingHintsException"><paramref name="hintTable"/> is malformed.</exception>
            internal MemberBindingHints( LuaTable hintTable, ref object target )
            {
                Type[] typeArgs = Array.ConvertAll(hintTable.RawToArray(), ( typeArg ) => typeArg as Type);

                foreach (var hintEntry in hintTable)
                {
                    string key = hintEntry.Key as string;

                    if (key == "SpecialName" && (hintEntry.Value == null || hintEntry.Value is bool))
                    {
                        RestrictAttributes(MethodAttributes.SpecialName, EventAttributes.SpecialName, FieldAttributes.SpecialName, TypeAttributes.SpecialName, PropertyAttributes.SpecialName, hintEntry.Value as bool?);
                        continue;
                    }
                    else if (hintEntry.Key is double)
                    {
                        // ignore type args
                        double doubleKey = (double)hintEntry.Key;
                        if (doubleKey % 1 == 0 && doubleKey > 0 && doubleKey <= typeArgs.Length)
                            continue;
                    }

                    throw new BindingHintsException(String.Format("Binding-hints table has unexpected hint '{0}'", hintEntry));
                }

                CLRStaticContext staticContext = target as CLRStaticContext;
                Type type = staticContext == null ? null : staticContext.ContextType;
                if (type != null && type.IsGenericTypeDefinition)
                {
                    try
                    {
                        target = new CLRStaticContext(type.MakeGenericType(typeArgs));
                    }
                    catch (ArgumentException ex)
                    {
                        // wrong number of type args or type arg constraints not satisfied
                        throw new BindingHintsException(String.Format("Binding-hints table has unexpected type arguments"), ex);
                    }
                }
                else if (typeArgs.Length > 0)
                {
                    throw new BindingHintsException("Binding-hints table has unexpected type arguments");
                }
            }

            /// <summary>
            /// Gets the types of members that are allow to be gotten.
            /// </summary>
            internal MemberTypes GetMemberTypes
            {
                get { return _memberTypes == 0 ? _allGetMemberTypes : _memberTypes & _allGetMemberTypes; }
            }

            /// <summary>
            /// Gets the types of member that are allow to be set.
            /// </summary>
            internal MemberTypes SetMemberTypes
            {
                get { return _memberTypes == 0 ? _allSetMemberTypes : _memberTypes & _allSetMemberTypes; }
            }

            private void RestrictAttributes( MethodAttributes methodAttribute, EventAttributes eventAttribute, FieldAttributes fieldAttribute, TypeAttributes typeAttribute, PropertyAttributes propertyAttribute, bool? restriction )
            {
                if (restriction == null)
                {
                    _methodAttributesMask   &= ~methodAttribute;
                    _eventAttributesMask    &= ~eventAttribute;
                    _fieldAttributesMask    &= ~fieldAttribute;
                    _typeAttributesMask     &= ~typeAttribute;
                    _propertyAttributesMask &= ~propertyAttribute;
                }
                else
                {
                    bool set = restriction.Value;

                    _methodAttributesMask   |= methodAttribute;
                    _eventAttributesMask    |= eventAttribute;
                    _fieldAttributesMask    |= fieldAttribute;
                    _typeAttributesMask     |= typeAttribute;
                    _propertyAttributesMask |= propertyAttribute;
                    _methodAttributes   = set ? _methodAttributes   | methodAttribute   : _methodAttributes   & ~methodAttribute;
                    _eventAttributes    = set ? _eventAttributes    | eventAttribute    : _eventAttributes    & ~eventAttribute;
                    _fieldAttributes    = set ? _fieldAttributes    | fieldAttribute    : _fieldAttributes    & ~fieldAttribute;
                    _typeAttributes     = set ? _typeAttributes     | typeAttribute     : _typeAttributes     & ~typeAttribute;
                    _propertyAttributes = set ? _propertyAttributes | propertyAttribute : _propertyAttributes & ~propertyAttribute;
                }
            }

            /// <summary>
            /// Filters out members that do not satisfy the member-binding hints.
            /// </summary>
            /// <param name="members">The array of candidate members.</param>
            /// <returns>The array of members that satisfy the member-binding hints.</returns>
            internal IEnumerable<MemberInfo> SelectHintedMembers( IEnumerable<MemberInfo> members )
            {
                foreach (var member in members)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Constructor:
                            ConstructorInfo constructor = member as ConstructorInfo;

                            if ((constructor.Attributes & _methodAttributesMask) != (_methodAttributes & _methodAttributesMask))
                                continue;

                            break;

                        case MemberTypes.Custom:
                            throw new InvalidOperationException("Should never happen!");

                        case MemberTypes.Event:
                            EventInfo @event = member as EventInfo;

                            if ((@event.Attributes & _eventAttributesMask) != (_eventAttributes & _eventAttributesMask))
                                continue;

                            break;

                        case MemberTypes.Field:
                            FieldInfo field = member as FieldInfo;

                            if ((field.Attributes & _fieldAttributesMask) != (_fieldAttributes & _fieldAttributesMask))
                                continue;

                            break;

                        case MemberTypes.Method:
                            MethodInfo method = member as MethodInfo;

                            if ((method.Attributes & _methodAttributesMask) != (_methodAttributes & _methodAttributesMask))
                                continue;

                            break;

                        case MemberTypes.NestedType:
                            Type type = member as Type;

                            if ((type.Attributes & _typeAttributesMask) != (_typeAttributes & _typeAttributesMask))
                                continue;

                            break;

                        case MemberTypes.Property:
                            PropertyInfo property = member as PropertyInfo;

                            if ((property.Attributes & _propertyAttributesMask) != (_propertyAttributes & _propertyAttributesMask))
                                continue;

                            break;

                        case MemberTypes.TypeInfo:
                            throw new InvalidOperationException("Should never happen!");

                        default:
                            throw new InvalidOperationException("Should never happen!");
                    }

                    yield return member;
                }
            }
        }

        /// <summary>
        /// Provides hints for resolving functions by signature.
        /// </summary>
        private struct SignatureBindingHints
        {
            private Type[] _typeArgs;

            private ParameterHint[] _paramHints;

            /// <summary>
            /// Initializes a new instance of the <see cref="SignatureBindingHints"/> struct from the entries in
            /// the specified Lua table.
            /// </summary>
            /// <param name="hintTable">The Lua table containing binding hints.</param>
            /// <exception cref="BindingHintsException"><paramref name="hintTable"/> is malformed.</exception>
            internal SignatureBindingHints( LuaTable hintTable )
            {
                _typeArgs = null;
                _paramHints = Array.ConvertAll(hintTable.RawToArray(), ( hint ) => new ParameterHint(hint));

                if (hintTable.Count > _paramHints.Length)
                {
                    foreach (var hintEntry in hintTable)
                    {
                        var key = hintEntry.Key as string;

                        if (key == "_" && hintEntry.Value is LuaTable)
                        {
                            var typeArgsTable = hintEntry.Value as LuaTable;
                            _typeArgs = Array.ConvertAll(typeArgsTable.RawToArray(), ( typeArg ) => typeArg as Type);
                            if (typeArgsTable.Count == _typeArgs.Length)
                                continue;
                        }
                        else if (hintEntry.Key is double)
                        {
                            // ignore parameter hints
                            double doubleKey = (double)hintEntry.Key;
                            if (doubleKey % 1 == 0 && doubleKey > 0 && doubleKey <= _paramHints.Length)
                                continue;
                        }

                        throw new BindingHintsException(String.Format("Binding-hints table has unexpected hint '{0}'", hintEntry));
                    }
                }
            }

            public bool IsSet
            {
                get { return _paramHints != null; }
            }

            internal IEnumerable<MethodBase> SelectHintedMethods( IEnumerable<MethodBase> methods )
            {
                if (_typeArgs != null)
                    methods = SelectTypeArgsSpecifiedMethods(methods);

                if (_paramHints != null && _paramHints.Length != 0)
                    methods = SelectParamHintedMethods(methods);

                return methods;
            }

            private IEnumerable<MethodBase> SelectTypeArgsSpecifiedMethods( IEnumerable<MethodBase> methods )
            {
                foreach (var methodBase in methods)
                {
                    if (!methodBase.IsGenericMethodDefinition)
                        continue;

                    switch (methodBase.MemberType)
                    {
                        case MemberTypes.Constructor:
                            continue;  // TODO: see LuaBinder.BindToMethod

                        case MemberTypes.Method:
                            MethodInfo method = methodBase as MethodInfo;
                            try
                            {
                                method = method.MakeGenericMethod(_typeArgs);
                            }
                            catch (ArgumentException)
                            {
                                // wrong number of type args or type arg constraints not satisfied
                                method = null;
                            }
                            if (method != null)
                                yield return method;
                            break;

                        default:
                            throw new InvalidOperationException("Should never happen!");
                    }
                }
            }

            private IEnumerable<MethodBase> SelectParamHintedMethods( IEnumerable<MethodBase> methods )
            {
                foreach (var methodBase in methods)
                    if (MatchesParameterTypeHints(methodBase.GetParameters()))
                        yield return methodBase;
            }

            private bool MatchesParameterTypeHints( ParameterInfo[] parameters )
            {
                if (parameters.Length != _paramHints.Length)
                    return false;

                for (int i = 0; i < _paramHints.Length; ++i)
                    if (!_paramHints[i].Matches(parameters[i]))
                        return false;

                return true;
            }

            private struct ParameterHint
            {
                private readonly String typeName;
                private readonly Type type;

                internal ParameterHint( object hint )
                {
                    if (hint is string)
                    {
                        typeName = hint as string;
                        type = null;
                    }
                    else if (hint is Type)
                    {
                        typeName = null;
                        type = hint as Type;
                    }
                    else
                    {
                        throw new BindingHintsException(String.Format("Parameter hint of unexpected type '{0}'", hint != null ? hint.GetType().ToString() : "null"));
                    }
                }

                internal bool Matches( ParameterInfo param )
                {
                    return type != null ?
                        param.ParameterType == type :
                        (param.ParameterType.FullName == typeName || param.ParameterType.Name == typeName);
                }
            }
        }
    }
}
