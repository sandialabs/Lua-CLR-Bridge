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
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using Lua;

    internal partial class ObjectTranslator
    {
        private const string _objectMetatableName = "CLI-object";

        private const string _partialMetatableName = "CLI-partial";

        /* The metamethod delegates must not be garbage collected until after the Lua state is closed.  Of
           particular importance is the GarbageCollect delegate, which releases CLI objects held by the Lua
           state while it is closing. */

        [SecurityCritical]
        private LuaLReg[] _objectMetamethods;

        [SecurityCritical]
        private LuaLReg[] _partialMetamethods;

        [SecurityCritical]
        private void InitializeMetamethods( IntPtr L )
        {
            _objectMetamethods = new LuaLReg[]
                {
                    new LuaLReg { name = "__add", func = ObjectAdd },
                    new LuaLReg { name = "__sub", func = ObjectSubtract },
                    new LuaLReg { name = "__mul", func = ObjectMultiply },
                    new LuaLReg { name = "__div", func = ObjectDivide },
                    new LuaLReg { name = "__mod", func = ObjectModulus },
                    new LuaLReg { name = "__unm", func = ObjectUnaryMinus },
                    new LuaLReg { name = "__eq", func = ObjectEqual },
                    new LuaLReg { name = "__lt", func = ObjectLessThan },
                    new LuaLReg { name = "__le", func = ObjectLessEqual },
                    new LuaLReg { name = "__index", func = ObjectIndex },
                    new LuaLReg { name = "__newindex", func = ObjectNewIndex },
                    new LuaLReg { name = "__call", func = ObjectCall },
                    new LuaLReg { name = "__tostring", func = ObjectToString },
                    new LuaLReg { name = "__gc", func = ObjectGarbageCollect },
                };

            _partialMetamethods = new LuaLReg[]
                {
                    new LuaLReg { name = "__index", func = PartialIndex },
                    new LuaLReg { name = "__newindex", func = PartialNewIndex },
                    new LuaLReg { name = "__call", func = PartialCall },
                    new LuaLReg { name = "__gc", func = PartialGarbageCollect },
                };

            CheckStack(L, 4);  // table + metatable + key + value

            LuaWrapper.lua_newtable(L); // empty table

            // create metatable for CLI objects
            LuaWrapper.luaL_newmetatable(L, _objectMetatableName, _encoding);
            LuaWrapper.luaL_setfuncs(L, _objectMetamethods, 0, _encoding);
            LuaWrapper.lua_pushstring(L, "__metatable", _encoding);
            LuaWrapper.lua_pushvalue(L, -3); // empty table
            LuaWrapper.lua_rawset(L, -3); // hide metatable
            LuaWrapper.lua_pop(L, 1);

            // create metatable for CLI partially-resolved methods and indexed properties
            LuaWrapper.luaL_newmetatable(L, _partialMetatableName, _encoding);
            LuaWrapper.luaL_setfuncs(L, _partialMetamethods, 0, _encoding);
            LuaWrapper.lua_pushstring(L, "__metatable", _encoding);
            LuaWrapper.lua_pushvalue(L, -3); // empty table
            LuaWrapper.lua_rawset(L, -3); // hide metatable
            LuaWrapper.lua_pop(L, 1);

            LuaWrapper.lua_pop(L, 1); // empty table
        }

        #region Invoking Operators

        [SecurityCritical]
        private int ObjectUnaryMinus( IntPtr L )
        {
            return ObjectUnaryOperator(L, "op_UnaryNegation");
        }

        [SecurityCritical]
        private int ObjectAdd( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_Addition");
        }

        [SecurityCritical]
        private int ObjectSubtract( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_Subtraction");
        }

        [SecurityCritical]
        private int ObjectMultiply( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_Multiply");
        }

        [SecurityCritical]
        private int ObjectDivide( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_Division");
        }

        [SecurityCritical]
        private int ObjectModulus( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_Modulus");
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectUnaryOperator( IntPtr L, string name )
        {
            try
            {
                var operand = ToObject(L, 1);

                return InvokeUnaryOperator(L, name, operand);
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.PreserveStackTrace();
                return Throw(L, ex.InnerException);
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectBinaryOperator( IntPtr L, string name )
        {
            try
            {
                var lhs = ToObject(L, 1);
                var rhs = ToObject(L, 2);

                return InvokeBinaryOperator(L, name, lhs, rhs);
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.PreserveStackTrace();
                return Throw(L, ex.InnerException);
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectEqual( IntPtr L )
        {
            try
            {
                var lhs = ToObject(L, 1);
                var rhs = ToObject(L, 2);

                try
                {
                    // for consistency because Lua won't invoke this function if the userdatas are the same
                    if (Object.ReferenceEquals(lhs, rhs))
                    {
                        LuaWrapper.lua_settop(L, 0);
                        /* no stack check -- not more results than arguments */

                        LuaWrapper.lua_pushboolean(L, true);
                        return 1;
                    }

                    return InvokeBinaryOperator(L, "op_Equality", lhs, rhs);
                }
                catch (MissingMethodException)
                {
                    if (lhs is Enum && rhs is Enum)
                    {
                        Type lhsType = lhs.GetType();
                        Type rhsType = rhs.GetType();

                        if (lhsType == rhsType)
                            return EnumEqual(L, lhs, lhsType, rhs, rhsType);
                    }

                    if (lhs != null && lhs.GetType().IsValueType)
                        throw;

                    if (rhs != null && rhs.GetType().IsValueType)
                        throw;

                    LuaWrapper.lua_settop(L, 0);
                    /* no stack check -- not more results than arguments */

                    LuaWrapper.lua_pushboolean(L, lhs == rhs);
                    return 1;
                }
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private static int EnumEqual( IntPtr L, object lhs, Type lhsType, object rhs, Type rhsType )
        {
            lhs = Convert.ChangeType(lhs, lhsType.GetEnumUnderlyingType());
            rhs = Convert.ChangeType(rhs, rhsType.GetEnumUnderlyingType());

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            LuaWrapper.lua_pushboolean(L, lhs.Equals(rhs));
            return 1;
        }

        private delegate int EnumLess( IntPtr L, object lhs, Type lhsType, object rhs, Type rhsType );

        [SecurityCritical]
        private int ObjectLessThan( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_LessThan", "op_GreaterThan", this.EnumLessThan);
        }

        [SecurityCritical]
        private int ObjectLessEqual( IntPtr L )
        {
            return ObjectBinaryOperator(L, "op_LessThanOrEqual", "op_GreaterThanOrEqual", this.EnumLessEqual);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectBinaryOperator( IntPtr L, string lessName, string greaterName, EnumLess enumLess )
        {
            try
            {
                var lhs = ToObject(L, 1);
                var rhs = ToObject(L, 2);

                try
                {
                    return InvokeBinaryOperator(L, lessName, lhs, rhs);
                }
                catch (MissingMethodException lessEx)
                {
                    try
                    {
                        return InvokeBinaryOperator(L, greaterName, rhs, lhs);
                    }
                    catch (MissingMethodException greaterEx)
                    {
                        if (lhs is Enum && rhs is Enum)
                        {
                            Type lhsType = lhs.GetType();
                            Type rhsType = rhs.GetType();

                            if (lhsType == rhsType)
                                return enumLess(L, lhs, lhsType, rhs, rhsType);
                        }

                        throw new MissingMethodException(String.Format("{0}; {1}", lessEx.Message, greaterEx.Message));
                    }
                }
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.PreserveStackTrace();
                return Throw(L, ex.InnerException);
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private int EnumLessThan( IntPtr L, object lhs, Type lhsType, object rhs, Type rhsType )
        {
            lhs = Convert.ChangeType(lhs, lhsType.GetEnumUnderlyingType());
            rhs = Convert.ChangeType(rhs, rhsType.GetEnumUnderlyingType());

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            LuaWrapper.lua_pushboolean(L, (lhs as IComparable).CompareTo(rhs) < 0);
            return 1;
        }

        [SecurityCritical]
        private int EnumLessEqual( IntPtr L, object lhs, Type lhsType, object rhs, Type rhsType )
        {
            lhs = Convert.ChangeType(lhs, lhsType.GetEnumUnderlyingType());
            rhs = Convert.ChangeType(rhs, rhsType.GetEnumUnderlyingType());

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            LuaWrapper.lua_pushboolean(L, (lhs as IComparable).CompareTo(rhs) <= 0);
            return 1;
        }

        [SecurityCritical]
        private int InvokeUnaryOperator( IntPtr L, string name, object operand )
        {
            MemberInfo[] members = new MemberInfo[0];

            Type operandType = operand.GetType();

            // rewrap Int64 and UInt64 so that operator overloads are available
            if (operandType.IsPrimitive)
            {
                if (operandType == typeof(Int64))
                {
                    operand = new CLRInt64((Int64)operand);
                    operandType = typeof(CLRInt64);
                }
                else if (operandType == typeof(UInt64))
                {
                    operand = new CLRUInt64((UInt64)operand);
                    operandType = typeof(CLRUInt64);
                }
            }

            ArrayUtility.Append(ref members, operandType.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static));

            MethodInfo[] methods = members
                .Select(member => member as MethodInfo)
                .Where(method => method.IsSpecialName)
                .ToArray();

            object[] args = new object[] { operand };

            try
            {
                if (members.Length == 0)
                    throw new MissingMethodException();

                object[] results = ObjectTranslator.InvokeMethod(null, name, methods, null, args);

                LuaWrapper.lua_settop(L, 0);
                CheckStack(L, results.Length);  // results

                foreach (object result in results)
                    PushObject(L, result);
                return results.Length;
            }
            catch (AmbiguousMatchException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                throw new AmbiguousMatchException(String.Format("'{1}({2})' designates ambiguous special members of type '{0}'", operandType, name, argTypes));
            }
            catch (MissingMethodException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                throw new MissingMethodException(String.Format("'{1}({2})' is not a special member of type '{0}'", operandType, name, argTypes));
            }
        }

        [SecurityCritical]
        private int InvokeBinaryOperator( IntPtr L, string name, object lhs, object rhs )
        {
            MemberInfo[] members = new MemberInfo[0];

            Type lhsType = lhs != null ? lhs.GetType() : null;
            Type rhsType = rhs != null ? rhs.GetType() : null;

            // rewrap Int64 and UInt64 so that operator overloads are available
            if (lhsType != null && lhsType.IsPrimitive && rhsType != null && rhsType.IsPrimitive)
            {
                if (lhsType == typeof(Int64))
                {
                    lhs = new CLRInt64((Int64)lhs);
                    lhsType = typeof(CLRInt64);
                }
                else if (lhsType == typeof(UInt64))
                {
                    lhs = new CLRUInt64((UInt64)lhs);
                    lhsType = typeof(CLRUInt64);
                }

                if (rhsType == typeof(Int64))
                {
                    rhs = new CLRInt64((Int64)rhs);
                    rhsType = typeof(CLRInt64);
                }
                else if (rhsType == typeof(UInt64))
                {
                    rhs = new CLRUInt64((UInt64)rhs);
                    rhsType = typeof(CLRUInt64);
                }
            }

            if (lhsType != null)
                ArrayUtility.Append(ref members, lhsType.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static));

            if (rhsType != null && rhsType != lhsType)
                ArrayUtility.Append(ref members, rhsType.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static));

            MethodInfo[] methods = members
                .Select(member => member as MethodInfo)
                .Where(method => method.IsSpecialName)
                .ToArray();

            object[] args = new object[] { lhs, rhs };

            try
            {
                if (members.Length == 0)
                    throw new MissingMethodException();

                object[] results = ObjectTranslator.InvokeMethod(null, name, methods, null, args);

                LuaWrapper.lua_settop(L, 0);
                CheckStack(L, results.Length);  // results

                foreach (object result in results)
                    PushObject(L, result);
                return results.Length;
            }
            catch (AmbiguousMatchException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                if (lhsType == null || rhsType == null || lhsType == rhsType)
                    throw new AmbiguousMatchException(String.Format("'{1}({2})' designates ambiguous special members of type '{0}'", lhsType ?? rhsType, name, argTypes));
                else
                    throw new AmbiguousMatchException(String.Format("'{2}({3})' designates ambiguous special members of types '{0}' and '{1}'", lhsType, rhsType, name, argTypes));
            }
            catch (MissingMethodException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                if (lhsType == null || rhsType == null || lhsType == rhsType)
                    throw new MissingMethodException(String.Format("'{1}({2})' is not a special member of type '{0}'", lhsType ?? rhsType, name, argTypes));
                else
                    throw new MissingMethodException(String.Format("'{2}({3})' is not a special member of type '{0}' or '{1}'", lhsType, rhsType, name, argTypes));
            }
        }

        #endregion

        #region Getting Members/Elements

        /// <summary>
        /// The metatable function for getting fields, methods, and properties of CLI objects/types and
        /// array elements of CLI array-objects.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The number of return values on the Lua stack.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectIndex( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _objectMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            object self;
            Type type;
            MemberBindingHints hints;
            UnwrapTarget(handle.Target, out self, out type, out hints);
            object index = ToObject(L, 2);

            try
            {
                if (index is string) // field, property, method
                {
                    object result = GetMember(type, self, hints, index as string);

                    LuaWrapper.lua_settop(L, 0);
                    /* no stack check -- not more results than arguments */

                    if (result is PartialTarget)
                        PushUntranslatedObject(L, result, _partialMetatableName);
                    else
                        PushObject(L, result);
                    return 1;
                }
                else if (self is Array) // array indexer
                {
                    object result = GetArrayElement(type, self as Array, index);

                    LuaWrapper.lua_settop(L, 0);
                    /* no stack check -- not more results than arguments */

                    PushObject(L, result);
                    return 1;
                }

                throw new TargetException(String.Format("'{1}' cannot be used to index into type '{0}'", type, index));
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private static object GetMember( Type type, object self, MemberBindingHints hints, string name )
        {
            Debug.Assert(self == null || type.IsInstanceOfType(self), "Type should match object.");

            if (hints == null)
                hints = MemberBindingHints.DefaultHints;

            BindingFlags bindingFlags = (self == null ? BindingFlags.Static : BindingFlags.Instance) |
                (LuaHideInheritedMembersAttribute.IsDefinedOn(type) ? BindingFlags.DeclaredOnly : BindingFlags.Default);
            IEnumerable<MemberInfo> membersTemp = type.GetMember(name,
                hints.GetMemberTypes & (self == null ? MemberTypes.All : ~MemberTypes.NestedType),  // hide NestedTypes if instance
                BindingFlags.Public | bindingFlags);

            membersTemp = hints.SelectHintedMembers(membersTemp);

            MemberInfo[] members = LuaBinder.RemoveHidden(membersTemp);

            if (members.Length == 0)
                goto notFound;

            int partialMemberCount = members.Count(member =>
                member.MemberType == MemberTypes.Event ||
                member.MemberType == MemberTypes.Method ||
                (member.MemberType == MemberTypes.Property && (member as PropertyInfo).GetIndexParameters().Length > 0));

            // return wrapper around partially-resolved members
            if (partialMemberCount > 0)
            {
                if (partialMemberCount != members.Length)
                    throw new AmbiguousMatchException(String.Format("'{0}.{1}' designates ambiguous members", type, name));

                return new PartialTarget(type, name, members, self);
            }

            return InvokeGet(type, name, members, self);

        notFound:
            throw new MissingMemberException(String.Format("'{1}' is not a member of type '{0}'", type, name));
        }

        [SecurityCritical]
        private static object InvokeGet( Type type, string name, MemberInfo[] members, object self )
        {
            Debug.Assert(members.Length != 0, "Cannot operate on zero members.");

            LuaBinder binder = LuaBinder.Instance;

            MemberInfo member;

            try
            {
                int nestedTypeMemberCount = members.Count(member_ => member_.MemberType == MemberTypes.NestedType);

                if (members.Length == 0)
                    throw new MissingMemberException();  // caught below

                if (nestedTypeMemberCount > 0)
                {
                    if (nestedTypeMemberCount != members.Length)
                        throw new AmbiguousMatchException();  // caught below

                    if (members.Length > 1)
                        throw new AmbiguousMatchException();  // caught below

                    member = members[0];
                }
                else
                {
                    object value = null;
                    member = binder.BindToFieldOrProperty(BindingFlags.GetField | BindingFlags.GetProperty, members, ref value, null);
                }
            }
            catch (AmbiguousMatchException)
            {
                throw new AmbiguousMatchException(String.Format("'{1}' designates ambiguous members of type '{0}'", type, name));
            }
            catch (MissingMemberException)
            {
                Debug.Assert(false, "Get binding should always match some member");
                throw new InvalidOperationException("Should never happen!");
            }

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (member as FieldInfo).GetValue(self);

                case MemberTypes.Property:
                    MethodInfo getMethod = (member as PropertyInfo).GetGetMethod(nonPublic: false);
                    if (getMethod == null)
                        throw new MethodAccessException(String.Format("'{0}.{1}' is not get-accessible", type, name));

                    try
                    {
                        return getMethod.Invoke(self, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        ex.InnerException.PreserveStackTrace();
                        throw ex.InnerException;
                    }

                case MemberTypes.NestedType:
                    return new CLRStaticContext(member as Type);

                default:
                    throw new InvalidOperationException(); // should never happen
            }
        }

        [SecurityCritical]
        private static object GetArrayElement( Type type, Array self, object indexObject )
        {
            object index = IndexToArrayIndex(type, indexObject);

            if (index is int) // one-dimension
            {
                return self.GetValue((int)index);
            }
            else if (index is int[]) // multi-dimension
            {
                return self.GetValue(index as int[]);
            }

            throw new InvalidOperationException(); // should never happen
        }

        #endregion

        #region Setting Members/Elements

        /// <summary>
        /// The metatable function for setting fields and properties of CLI objects/types and array elements
        /// of CLI array-objects.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The number of return values on the Lua stack.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectNewIndex( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _objectMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            object self;
            Type type;
            MemberBindingHints hints;
            UnwrapTarget(handle.Target, out self, out type, out hints);
            object index = ToObject(L, 2);
            object value = ToObject(L, 3);

            try
            {
                if (index is string) // field, property
                {
                    SetMember(type, self, hints, index as string, value);

                    LuaWrapper.lua_settop(L, 0);
                    return 0;
                }
                else if (self is Array) // array indexer
                {
                    SetArrayElement(type, self as Array, index, value);

                    LuaWrapper.lua_settop(L, 0);
                    return 0;
                }

                throw new TargetException(String.Format("'{1}' cannot be used to index into type '{0}'", type, index));
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private static void SetMember( Type type, object self, MemberBindingHints hints, string name, object value )
        {
            Debug.Assert(self == null || type.IsInstanceOfType(self), "Type should match object.");

            if (hints == null)
                hints = MemberBindingHints.DefaultHints;

            BindingFlags bindingFlags = (self == null ? BindingFlags.Static : BindingFlags.Instance) |
                (LuaHideInheritedMembersAttribute.IsDefinedOn(type) ? BindingFlags.DeclaredOnly : BindingFlags.Default);
            IEnumerable<MemberInfo> membersTemp = type.GetMember(name,
                hints.SetMemberTypes,
                BindingFlags.Public | bindingFlags);

            membersTemp = hints.SelectHintedMembers(membersTemp);

            MemberInfo[] members = LuaBinder.RemoveHidden(membersTemp);

            if (members.Length == 0)
                goto notFound;

            int partialMemberCount = members.Count(member =>
                member.MemberType == MemberTypes.Property && (member as PropertyInfo).GetIndexParameters().Length > 0);

            // if setting partially-resolved member, fail
            // (setting indexed properties happens through getMember)
            if (partialMemberCount > 0)
            {
                if (partialMemberCount != members.Length)
                    throw new AmbiguousMatchException(String.Format("'{0}.{1}' designates ambiguous members", type, name));

                throw new TargetException(String.Format("'{1}' is not an assignable member of type '{0}'", type, name));
            }

            InvokeSet(type, name, members, self, value);
            return;

        notFound:
            throw new MissingMemberException(String.Format("'{1}' is not a member of type '{0}'", type, name));
        }

        [SecurityCritical]
        private static void InvokeSet( Type type, string name, MemberInfo[] members, object self, object value )
        {
            Debug.Assert(members.Length != 0, "Cannot operate on zero members.");

            LuaBinder binder = LuaBinder.Instance;

            MemberInfo member;

            try
            {
                member = binder.BindToFieldOrProperty(BindingFlags.SetField | BindingFlags.SetProperty, members, ref value, null);
            }
            catch (AmbiguousMatchException)
            {
                throw new AmbiguousMatchException(String.Format("'{1}' designates ambiguous members of type '{0}'", type, name));
            }
            catch (MissingMemberException)
            {
                string valueType = value == null ? "null" : value.GetType().ToString();
                throw new InvalidCastException(String.Format("'{0}.{1}' cannot be assigned a value of type '{2}'", type, name, valueType));
            }

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    FieldInfo field = member as FieldInfo;

                    field.SetValue(self, value);
                    return;

                case MemberTypes.Property:
                    PropertyInfo property = member as PropertyInfo;

                    MethodInfo setMethod = property.GetSetMethod(nonPublic: false);
                    if (setMethod == null)
                        throw new MethodAccessException(String.Format("'{0}.{1}' is not set-accessible", type, name));

                    try
                    {
                        setMethod.Invoke(self, new[] { value });
                        return;
                    }
                    catch (TargetInvocationException ex)
                    {
                        ex.InnerException.PreserveStackTrace();
                        throw ex.InnerException;
                    }

                default:
                    throw new InvalidOperationException(); // should never happen
            }
        }

        [SecurityCritical]
        private static void SetArrayElement( Type type, Array self, object indexObject, object value )
        {
            object index = IndexToArrayIndex(type, indexObject);

            try
            {
                value = LuaBinder.Instance.ChangeType(value, type.GetElementType(), null);
            }
            catch (InvalidCastException)
            {
                string valueType = value == null ? "null" : value.GetType().ToString();
                throw new InvalidCastException(String.Format("Element of type '{0}' cannot be assigned a value of type '{1}'", type, valueType));
            }

            if (index is int) // one-dimension
            {
                self.SetValue(value, (int)index);
                return;
            }
            else if (index is int[]) // multi-dimension
            {
                self.SetValue(value, index as int[]);
                return;
            }

            throw new InvalidOperationException(); // should never happen
        }

        #endregion

        #region Invoking Constructor (or adding binding hints)

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectCall( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _objectMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            object self;
            Type type;
            MemberBindingHints hints;
            UnwrapTarget(handle.Target, out self, out type, out hints);

            try
            {
                // add binding hints for subsequent get/set member
                if (hints == null &&
                    LuaWrapper.lua_gettop(L) == 2 &&
                    LuaWrapper.lua_type(L, 2) == LuaType.LUA_TTABLE)
                {
                    LuaTable hintTable = ToObject(L, 2) as LuaTable;

                    object target = handle.Target;
                    hints = new MemberBindingHints(hintTable, ref target);

                    LuaWrapper.lua_settop(L, 0);
                    /* no stack check -- not more results than arguments */

                    PushUntranslatedObject(L, new WrappedTarget(target, hints));
                    return 1;
                }
                else if (self != null)
                {
                    if (type.IsDelegate())
                    {
                        MethodInfo method = type.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

                        return InvokeMethod(L, self.GetType(), "Invoke", new[] { method }, self);
                    }

                    throw new ObjectTranslatorException(String.Format("Object of type '{0}' cannot be called", type));
                }

                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                var result = new PartialTarget(type, type.Name, constructors, self);

                CheckStack(L, 1);

                PushUntranslatedObject(L, result, _partialMetatableName);
                LuaWrapper.lua_replace(L, 1);
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }

            return PartialCall(L);
        }

        #endregion

        /// <summary>
        /// The metatable function for translating CLI objects/types to a string representation.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The number of return values on the Lua stack.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int ObjectToString( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _objectMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            object self;
            Type type;
            MemberBindingHints hints;
            UnwrapTarget(handle.Target, out self, out type, out hints);

            try
            {
                LuaWrapper.lua_settop(L, 0);
                /* no stack check -- not more results than arguments */

                LuaWrapper.lua_pushstring(L, self.ToString(), _encoding);
                return 1;
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private int ObjectGarbageCollect( IntPtr L )
        {
            return GarbageCollect(L, _objectMetatableName);
        }

        private static void UnwrapTarget( object target, out object self, out Type type, out MemberBindingHints hints )
        {
            if (target is WrappedTarget)
            {
                WrappedTarget hinted = target as WrappedTarget;
                target = hinted._target;
                hints = hinted._hints;
            }
            else
            {
                hints = null;
            }

            if (target is CLRStaticContext)
            {
                self = null;
                type = (target as CLRStaticContext).ContextType;
            }
            else
            {
                self = target;
                type = self.GetType();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Root cause of failure is not particularly important.")]
        private static object IndexToArrayIndex( Type type, object indexObject )
        {
            if (indexObject is double) // one-dimension
            {
                int index;
                try
                {
                    double indexDouble = (double)indexObject;
                    if (indexDouble % 1 == 0)
                        index = Convert.ToInt32(indexDouble);
                    else
                        throw new ObjectTranslatorException("Array index must be an integer");
                }
                catch (SEHException)
                {
                    throw;  // Lua internal; not for us
                }
                catch (Exception)
                {
                    goto invalidIndex;
                }

                return index;
            }
            else if (indexObject is LuaTable) // multi-dimension
            {
                int[] index;
                try
                {
                    index = Array.ConvertAll((indexObject as LuaTable).RawToArray(), ( element ) =>
                    {
                        if (element is double)
                        {
                            double elementDouble = (double)element;
                            if (elementDouble % 1 == 0)
                                return Convert.ToInt32(elementDouble);
                        }

                        throw new ObjectTranslatorException("Array index must be an integer");
                    });
                }
                catch (SEHException)
                {
                    throw;  // Lua internal; not for us
                }
                catch (Exception)
                {
                    goto invalidIndex;
                }

                return index;
            }

        invalidIndex:
            throw new ArgumentOutOfRangeException("indexObject", indexObject, String.Format("'{1}' is not a valid index for type '{0}'", type, indexObject));
        }

        #region Invoking Indexed Properties

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int PartialIndex( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _partialMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            PartialTarget self = handle.Target as PartialTarget;

            MethodBase[] methods;
            object index = ToObject(L, 2);

            try
            {
                IEnumerable<MethodBase> methodsTemp = self._members
                    .Select(member => member as PropertyInfo)
                    .Where(member => member != null)
                    .Select(property => property.GetGetMethod(nonPublic: false))
                    .Where(method => method != null);

                methods = self._hints.SelectHintedMethods(methodsTemp)
                    .ToArray();

                var indexes = index as LuaTable;
                object[] args = indexes != null ?
                    indexes.RawToArray() :
                    new object[] { index };

                object[] results = InvokeMethod(self._type, "get_" + self._name, methods, self._self, args);

                Debug.Assert(results.Length == 1, "Property getter should have a single result.");

                LuaWrapper.lua_settop(L, 0);
                /* no stack check -- not more results than arguments */

                PushObject(L, results[0]);
                return 1;
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int PartialNewIndex( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _partialMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            PartialTarget self = handle.Target as PartialTarget;

            MethodBase[] methods;
            object index = ToObject(L, 2);
            object value = ToObject(L, 3);

            try
            {
                IEnumerable<MethodBase> methodsTemp = self._members
                    .Select(member => member as PropertyInfo)
                    .Where(member => member != null)
                    .Select(property => property.GetSetMethod(nonPublic: false))
                    .Where(method => method != null);

                methods = self._hints.SelectHintedMethods(methodsTemp)
                    .ToArray();

                var indexes = index as LuaTable;
                object[] args;
                if (indexes != null)
                {
                    args = indexes.RawToArray();
                    Array.Resize(ref args, args.Length + 1);
                    args[args.Length - 1] = value;
                }
                else
                {
                    args = new object[] { index, value };
                }

                object[] results = InvokeMethod(self._type, "set_" + self._name, methods, self._self, args);

                Debug.Assert(results.Length == 0, "Property setter should have no results.");

                LuaWrapper.lua_settop(L, 0);
                return 0;
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        #endregion

        #region Invoking Methods (or adding binding hints)

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int PartialCall( IntPtr L )
        {
            IntPtr udata = LuaWrapper.luaL_testudata(L, 1, _partialMetatableName, _encoding);
            Debug.Assert(udata != IntPtr.Zero, "Should only be invoked on appropriate userdata.");

            GCHandle handle = (GCHandle)Marshal.PtrToStructure(udata, typeof(GCHandle));

            PartialTarget self = handle.Target as PartialTarget;

            MethodBase[] methods;

            try
            {
                // add binding hints for subsequent call
                if (!self._hints.IsSet &&
                    LuaWrapper.lua_gettop(L) == 2 &&
                    LuaWrapper.lua_type(L, 2) == LuaType.LUA_TTABLE)
                {
                    LuaTable hintTable = ToObject(L, 2) as LuaTable;
                    self._hints = new SignatureBindingHints(hintTable);

                    LuaWrapper.lua_settop(L, 0);
                    /* no stack check -- not more results than arguments */

                    PushUntranslatedObject(L, self, _partialMetatableName);
                    return 1;
                }

                IEnumerable<MethodBase> methodsTemp = self._members
                    .Select(member => member as MethodBase)
                    .Where(member => member != null);

                methods = self._hints.SelectHintedMethods(methodsTemp)
                    .ToArray();
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }

            return InvokeMethod(L, self._type, self._name, methods, self._self);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is stashed on Lua stack.")]
        [SecurityCritical]
        private int InvokeMethod( IntPtr L, Type type, string name, MethodBase[] methods, object self )
        {
            try
            {
                int argCount = LuaWrapper.lua_gettop(L) - 1;

                object[] args = new object[argCount];
                for (int i = 0; i < argCount; ++i)
                    args[i] = ToObject(L, i + 2);

                object[] results = InvokeMethod(type, name, methods, self, args);

                LuaWrapper.lua_settop(L, 0);
                CheckStack(L, results.Length);  // results

                foreach (object result in results)
                    PushObject(L, result);
                return results.Length;
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.PreserveStackTrace();
                return Throw(L, ex.InnerException);
            }
            catch (SEHException)
            {
                throw;  // Lua internal; not for us
            }
            catch (Exception ex)
            {
                return Throw(L, ex);
            }
        }

        [SecurityCritical]
        private static object[] InvokeMethod( Type type, string name, MethodBase[] methods, object self, object[] args )
        {
            Binder binder = LuaBinder.Instance;

            MethodBase method;
            object state;

            try
            {
                if (methods.Length == 0)
                    throw new MissingMethodException();

                method = binder.BindToMethod(0, methods, ref args, null, null, null, out state);
            }
            catch (AmbiguousMatchException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                throw new AmbiguousMatchException(String.Format("'{1}({2})' designates ambiguous members of type '{0}'", type, name, argTypes));
            }
            catch (MissingMethodException)
            {
                string argTypes = String.Join(", ", args.Select(o => o == null ? "null" : o.GetType().ToString()));
                throw new MissingMethodException(String.Format("'{1}({2})' is not a member of type '{0}'", type, name, argTypes));
            }

            List<object> results = new List<object>();

            object result = method is ConstructorInfo ?
                (method as ConstructorInfo).Invoke(args) :
                method.Invoke(self, args);

            if (!(method is MethodInfo) || (method as MethodInfo).ReturnType != typeof(void))
                results.Add(result);

            if (state != null)
                binder.ReorderArgumentArray(ref args, state);

            ParameterInfo[] methodParams = method.GetParameters();
            for (int i = 0; i < methodParams.Length; ++i)
                if (methodParams[i].ParameterType.IsByRef)
                    results.Add(args[i]);

            return results.ToArray();
        }

        #endregion

        [SecurityCritical]
        private int PartialGarbageCollect( IntPtr L )
        {
            return GarbageCollect(L, _partialMetatableName);
        }

        [SecuritySafeCritical]
        internal bool TryGetEvent( LuaUserData eventUserData, out Type type, out string name, out EventInfo[] events, out object self )
        {
            PartialTarget partialTarget;

            using (var lockedMainL = LockedMainState)
            {
                var L = lockedMainL._L;

                CheckStack(L, 1);

                eventUserData.Push(L);
                partialTarget = ToUntranslatedObject(L, -1, _partialMetatableName) as PartialTarget;
                LuaWrapper.lua_pop(L, 1);
            }

            if (partialTarget == null)
            {
                type = null;
                name = null;
                events = null;
                self = null;

                return false;
            }

            type = partialTarget._type;
            name = partialTarget._name;

            events = partialTarget._members
                .Select(member => member as EventInfo)
                .Where(member => member != null)
                .ToArray();

            self = partialTarget._self;

            return true;
        }

        [SecuritySafeCritical]
        internal bool TryGetMethodGroup( LuaUserData methodGroupUserData, out Type type, out string name, out MethodInfo[] methods, out object self )
        {
            PartialTarget partialTarget;

            using (var lockedMainL = LockedMainState)
            {
                var L = lockedMainL._L;

                CheckStack(L, 1);

                methodGroupUserData.Push(L);
                partialTarget = ToUntranslatedObject(L, -1, _partialMetatableName) as PartialTarget;
                LuaWrapper.lua_pop(L, 1);
            }

            if (partialTarget == null)
            {
                type = null;
                name = null;
                methods = null;
                self = null;

                return false;
            }

            type = partialTarget._type;
            name = partialTarget._name;

            IEnumerable<MethodBase> methodsTemp = partialTarget._members
                .Select(member => member as MethodInfo)
                .Where(member => member != null);

            methods = partialTarget._hints.SelectHintedMethods(methodsTemp)
                .Select(member => member as MethodInfo)
                .ToArray();

            self = partialTarget._self;

            return true;
        }

        /// <summary>
        /// Represents an object that has had member-binding hints specified.
        /// </summary>
        private class WrappedTarget
        {
            internal readonly object _target;

            internal readonly MemberBindingHints _hints;

            internal WrappedTarget( object target, MemberBindingHints hints )
            {
                this._target = target;
                this._hints = hints;
            }
        }

        /// <summary>
        /// Represents partially-resolved members (ex. a method group or an indexed property) that have been
        /// accessed in Lua but cannot be resolved until further discrimination (parameters or an index) is
        /// provided.  The members may have had signature-binding hints specified.
        /// </summary>
        private class PartialTarget
        {
            internal readonly Type _type;
            internal readonly string _name;
            internal readonly IEnumerable<MemberInfo> _members;
            internal readonly object _self;

            internal SignatureBindingHints _hints;

            public PartialTarget( Type type, string name, IEnumerable<MemberInfo> members, object self )
            {
                this._type = type;
                this._name = name;
                this._members = members;
                this._self = self;
            }
        }
    }
}
