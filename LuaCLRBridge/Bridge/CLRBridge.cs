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
    using System.Reflection;
    using System.Security;
    using Lua;

    /// <summary>
    /// Provides an interface in Lua to some CLR functionalities.
    /// </summary>
    [LuaHideInheritedMembers]
    public sealed class CLRBridge : MarshalByRefObject, IDisposable
    {
        private bool _disposed = false;

        [SecurityCritical]
        private readonly ObjectTranslator _objectTranslator;

        /// <summary>
        /// A wrapper for type-converting casts.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua.Do("assert(CLR.Cast.Int32(1/0) == -2147483648)");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "CLRStaticContext is immutable.")]
        public readonly CLRStaticContext Cast = new CLRStaticContext(typeof(CastHelper));

        /// <summary>
        /// The look-up table for CLI types.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua.Do("assert(CLR.Is('Hello!', CLR.Type['System.String']))");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This LuaTable is immutable.")]
        public readonly LuaTable Type;

        /// <summary>
        /// The look-up table for CLI static contexts.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua.Do("CLR.Static['System.Console'].WriteLine('Hello!')");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This LuaTable is immutable.")]
        public readonly LuaTable Static;

        /// <summary>
        /// Gets the Lua iterator for <see cref="System.Collections.IEnumerable"/>.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua["s"] = new HashSet&lt;string&gt; { "a", "b", "c" };
        ///     lua.Do("t = {}; for v in CLR.Items(s) do t[v] = v end");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Delegates are immutable.")]
        public readonly LuaCFunction Items;

        /// <summary>
        /// Gets the Lua iterator for 1-dimensional <see cref="Array"/>.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua["a"] = new string[] { "a", "b", "c" }
        ///     lua.Do("t = {}; for k, v in CLR.IPairs(a) do t[k] = v end");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Delegates are immutable.")]
        public readonly LuaCFunction IPairs;

#pragma warning disable 1584,1658

        /// <summary>
        /// Gets the Lua iterator for <see cref="IEnumerator&lt;KeyValuePair&lt;,&gt;&gt;"/>.
        /// </summary>
        /// <remarks/><!-- for doxygen -->
        /// <example>
        /// <code>
        /// using (var lua = new LuaCLRBridge.LuaBridge())
        /// {
        ///     lua["d"] = new Dictionary&lt;string, int&gt; { { "a", 1 }, { "b", 2 }, { "c", 3 } };
        ///     lua.Do("t = {}; for k, v in CLR.Pairs(d) do t[k] = v end");
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Only used from Lua where field access is slightly more performant.")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Delegates are immutable.")]
        public readonly LuaCFunction Pairs;

#pragma warning restore 1584,1658

        private readonly LuaCFunction _itemsNext;

        private readonly LuaCFunction _iPairsNext;

        private readonly LuaCFunction _pairsNext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CLRBridge"/> class.
        /// </summary>
        /// <param name="objectTranslator">The object translator associated with the state that the bridge
        ///     will exist within.</param>
        [SecuritySafeCritical]
        internal CLRBridge( ObjectTranslator objectTranslator )
        {
            _objectTranslator = objectTranslator;

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                Type = MakeLookUpTable(L, TypeIndex);
                Static = MakeLookUpTable(L, StaticIndex);
            }

            /* cache delegates */

            Items = ItemsInit;
            _itemsNext = ItemsNext;

            IPairs = IPairsInit;
            _iPairsNext = IPairsNext;

            Pairs = PairsInit;
            _pairsNext = PairsNext;
        }

        /// <summary>
        /// Ensures that the resources are freed and other cleanup operations are performed when the garbage
        /// collector reclaims the <see cref="CLRBridge"/>.
        /// </summary>
        ~CLRBridge()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all the resources used by the <see cref="CLRBridge"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="CLRBridge"/> and optionally releases the
        /// managed resources.
        /// </summary>
        /// <param name="disposeManaged">If <c>true</c>, releases both managed and unmanaged resources;
        ///     otherwise releases only unmanaged resources.</param>
        private void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposeManaged)
            {
                if (Type != null)
                    Type.Dispose();
                if (Static != null)
                    Static.Dispose();
            }
        }

        #region Collection conversion

        /// <summary>
        /// Translates an <see cref="IDictionary&lt;K, V&gt;"/> to a <see cref="LuaTable"/>.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>A table containing the entries from the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.
        ///     </exception>
        [SecuritySafeCritical]
        public LuaTable ToTable<TKey, TValue>( IDictionary<TKey, TValue> dictionary )
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            LuaTable table = LuaTable.Create(_objectTranslator);

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 3);  // table + key + value

                table.Push(L);

                foreach (var entry in dictionary)
                {
                    _objectTranslator.PushObject(L, entry.Key);
                    _objectTranslator.PushObject(L, entry.Value);

                    LuaWrapper.lua_settable(L, -3);
                }

                LuaWrapper.lua_pop(L, 1);  // table
            }

            return table;
        }

        /// <summary>
        /// Translates an <see cref="IList&lt;T&gt;"/> to a <see cref="LuaTable"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="initialIndex">The table index of the first element from the list.</param>
        /// <returns>A table containing the elements from the list as values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <c>null</c>.</exception>
        [SecuritySafeCritical]
        public LuaTable ToTable<T>( IList<T> list, int initialIndex = 1 )
        {
            if (list == null)
                throw new ArgumentNullException("list");

            LuaTable table = LuaTable.Create(_objectTranslator, list.Count, 0);

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 3);  // table + index + element

                table.Push(L);

                int index = initialIndex;
                foreach (var element in list)
                {
                    LuaWrapper.lua_pushinteger(L, index);
                    _objectTranslator.PushObject(L, element);

                    LuaWrapper.lua_settable(L, -3);

                    ++index;
                }

                LuaWrapper.lua_pop(L, 1);  // table
            }

            return table;
        }

        /// <summary>
        /// Translates an <see cref="Array"/> to a <see cref="LuaTable"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="initialIndex">The table index of the first element from the array.</param>
        /// <returns>A table containing the elements from the list as values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="array"/> is a multi-dimensional array.
        ///     </exception>
        [SecuritySafeCritical]
        public LuaTable ToTable( Array array, int initialIndex = 1 )
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("Must be 1-dimensional", "array");

            int length = array.Length;

            LuaTable table = LuaTable.Create(_objectTranslator, length, 0);

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 3);  // table + index + value

                table.Push(L);

                for (int index = 0; index < length; ++index)
                {
                    LuaWrapper.lua_pushinteger(L, index + initialIndex);
                    _objectTranslator.PushObject(L, array.GetValue(index));

                    LuaWrapper.lua_settable(L, -3);
                }

                LuaWrapper.lua_pop(L, 1);  // table
            }

            return table;
        }

        /// <summary>
        /// Translates an <see cref="ISet&lt;T&gt;"/> to a <see cref="LuaTable"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="set">The set.</param>
        /// <returns>A table containing the elements of the set as both keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        [SecuritySafeCritical]
        public LuaTable ToTable<T>( ISet<T> set )
        {
            if (set == null)
                throw new ArgumentNullException("set");

            LuaTable table = LuaTable.Create(_objectTranslator);

            using (var lockedMainL = _objectTranslator.LockedMainState)
            {
                var L = lockedMainL._L;

                ObjectTranslator.CheckStack(L, 3);  // table + element + element

                table.Push(L);

                foreach (var element in set)
                {
                    _objectTranslator.PushObject(L, element);
                    LuaWrapper.lua_pushvalue(L, -1);

                    LuaWrapper.lua_settable(L, -3);
                }

                LuaWrapper.lua_pop(L, 1);  // table
            }

            return table;
        }

        #endregion

        #region Types

        [SecurityCritical]
        private int TypeIndex( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 2);

            Type type = null;

            if (LuaWrapper.lua_isuserdata(L, 2))
            {
                var staticContext = _objectTranslator.ToUntranslatedObject(L, 2) as CLRStaticContext;

                if (staticContext != null)
                    type = staticContext.ContextType;
            }

            if (type == null)
            {
                string typeName = LuaWrapper.luaL_checkstring(L, 2, _objectTranslator.Encoding);

                type = LookUpType(typeName);
            }

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            if (type == null)
                LuaWrapper.lua_pushnil(L);
            else
                _objectTranslator.PushUntranslatedObject(L, type);
            return 1;
        }

        [SecurityCritical]
        private int StaticIndex( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 2);

            Type type = null;

            if (LuaWrapper.lua_isuserdata(L, 2))
            {
                type = _objectTranslator.ToUntranslatedObject(L, 2) as Type;
            }

            if (type == null)
            {
                string typeName = LuaWrapper.luaL_checkstring(L, 2, _objectTranslator.Encoding);

                type = LookUpType(typeName);
            }

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            if (type == null)
                LuaWrapper.lua_pushnil(L);
            else
                _objectTranslator.PushUntranslatedObject(L, new CLRStaticContext(type));
            return 1;
        }

        private static Type LookUpType( string typeName )
        {
            Type result = System.Type.GetType(typeName);

            if (result == null || !result.IsVisible)
            {
                List<Type> types = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType(typeName);
                    if (type != null && type.IsVisible)
                        types.Add(type);
                }

                switch (types.Count)
                {
                    case 0:
                        result = null;
                        break;
                    case 1:
                        result = types[0];
                        break;
                    default:
                        throw new CLRBridgeException(String.Format("The type name '{0}' is ambiguous", typeName));
                }
            }

            return result;
        }

        [SecurityCritical]
        private LuaTable MakeLookUpTable( IntPtr L, LuaCFunction index )
        {
            ObjectTranslator.CheckStack(L, 4);  // table + metatable + key + value

            LuaWrapper.lua_newtable(L);

            LuaWrapper.lua_newtable(L);
            LuaWrapper.lua_pushstring(L, "__index", _objectTranslator.Encoding);
            _objectTranslator.PushCFunctionDelegate(L, index);
            LuaWrapper.lua_rawset(L, -3);
            LuaWrapper.lua_pushstring(L, "__newindex", _objectTranslator.Encoding);
            _objectTranslator.PushCFunctionDelegate(L, ReadOnlyNewIndex);
            LuaWrapper.lua_rawset(L, -3);
            LuaWrapper.lua_pushstring(L, "__metatable", _objectTranslator.Encoding);
            LuaWrapper.lua_pushvalue(L, -3);
            LuaWrapper.lua_rawset(L, -3); // hide metatable
            LuaWrapper.lua_setmetatable(L, -2);

            return _objectTranslator.PopObject(L) as LuaTable;
        }

        [SecurityCritical]
        private int ReadOnlyNewIndex( IntPtr L )
        {
            LuaWrapper.lua_settop(L, 0);
            Debug.Assert(LuaWrapper.LUA_MINSTACK >= 1, "Insufficient stack.");

            LuaWrapper.lua_pushstring(L, "Cannot modify read-only table.", _objectTranslator.Encoding);
            return LuaWrapper.lua_error(L);
        }

        #endregion

        #region Iteration

        [SecurityCritical]
        private int ItemsInit( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);

            var enumerable = _objectTranslator.ToObject(L, 1) as System.Collections.IEnumerable;
            if (enumerable == null)
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerable", _objectTranslator.Encoding);

            LuaWrapper.lua_settop(L, 0);
            Debug.Assert(LuaWrapper.LUA_MINSTACK >= 3, "Insufficient stack.");

            _objectTranslator.PushCFunctionDelegate(L, _itemsNext);
            _objectTranslator.PushObject(L, enumerable.GetEnumerator());
            LuaWrapper.lua_pushvalue(L, -1);
            return 3;
        }

        [SecurityCritical]
        private int ItemsNext( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.luaL_checkany(L, 2);

            var enumerator = _objectTranslator.ToObject(L, 1) as System.Collections.IEnumerator;
            if (enumerator == null)
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerator", _objectTranslator.Encoding);

            if (!enumerator.MoveNext())
            {
                LuaWrapper.lua_settop(L, 0);
                /* no stack check -- not more results than arguments */

                LuaWrapper.lua_pushnil(L);
                return 1;
            }

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            _objectTranslator.PushObject(L, enumerator.Current);
            return 1;
        }

        [SecurityCritical]
        private int IPairsInit( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);

            var array = _objectTranslator.ToObject(L, 1) as Array;
            if (array == null)
                LuaWrapper.luaL_argerror(L, 1, "expected Array", _objectTranslator.Encoding);
            if (array.Rank != 1)
                LuaWrapper.luaL_argerror(L, 1, "expected 1-dimensional Array", _objectTranslator.Encoding);

            LuaWrapper.lua_settop(L, 0);
            Debug.Assert(LuaWrapper.LUA_MINSTACK >= 3, "Insufficient stack.");

            _objectTranslator.PushCFunctionDelegate(L, _iPairsNext);
            _objectTranslator.PushObject(L, array.GetEnumerator());
            LuaWrapper.lua_pushinteger(L, -1);
            return 3;
        }

        [SecurityCritical]
        private int IPairsNext( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            int index = LuaWrapper.luaL_checkint(L, 2);

            var enumerator = _objectTranslator.ToObject(L, 1) as System.Collections.IEnumerator;
            if (enumerator == null)
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerator", _objectTranslator.Encoding);

            if (!enumerator.MoveNext())
            {
                LuaWrapper.lua_settop(L, 0);
                /* no stack check -- not more results than arguments */

                LuaWrapper.lua_pushnil(L);
                return 1;
            }

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            LuaWrapper.lua_pushinteger(L, index + 1);
            _objectTranslator.PushObject(L, enumerator.Current);
            return 2;
        }

        [SecurityCritical]
        private int PairsInit( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);

            var enumerable = _objectTranslator.ToObject(L, 1) as System.Collections.IEnumerable;
            if (enumerable == null)
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerable", _objectTranslator.Encoding);

            LuaWrapper.lua_settop(L, 0);
            Debug.Assert(LuaWrapper.LUA_MINSTACK >= 3, "Insufficient stack.");

            _objectTranslator.PushCFunctionDelegate(L, _pairsNext);
            _objectTranslator.PushObject(L, enumerable.GetEnumerator());
            LuaWrapper.lua_pushinteger(L, -1);
            return 3;
        }

        [SecurityCritical]
        private int PairsNext( IntPtr L )
        {
            LuaWrapper.luaL_checkany(L, 1);
            LuaWrapper.luaL_checkany(L, 2);

            var enumerator = _objectTranslator.ToObject(L, 1) as System.Collections.IEnumerator;
            if (enumerator == null)
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerator", _objectTranslator.Encoding);

            if (!enumerator.MoveNext())
            {
                LuaWrapper.lua_settop(L, 0);
                /* no stack check -- not more results than arguments */

                LuaWrapper.lua_pushnil(L);
                return 1;
            }

            object pair = enumerator.Current;
            Type pairType = pair.GetType();
            if (pairType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                LuaWrapper.luaL_argerror(L, 1, "expected IEnumerator<KeyValuePair<,>>", _objectTranslator.Encoding);

            LuaWrapper.lua_settop(L, 0);
            /* no stack check -- not more results than arguments */

            _objectTranslator.PushObject(L, pairType.GetProperty("Key").GetValue(pair, null));
            _objectTranslator.PushObject(L, pairType.GetProperty("Value").GetValue(pair, null));
            return 2;
        }

        #endregion

        #region Casting

        /// <summary>
        /// Checks if a specified object is an instance of a specified type.
        /// </summary>
        /// <param name="object">The object to be checked.</param>
        /// <param name="type">The type to check <paramref name="object"/> for.</param>
        /// <returns><c>true</c> if the object is an instance of the type; otherwise, <c>false</c>.
        ///     </returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Instance method is more convenient for use from Lua.")]
        public bool Is( object @object, Type type )
        {
            return type.IsInstanceOfType(@object);
        }

        /// <summary>
        /// Casts a specified object to a specified type.
        /// </summary>
        /// <param name="object">The object to be cast.</param>
        /// <param name="type">The type to cast <paramref name="object"/> to.</param>
        /// <returns><paramref name="object"/> if the object is an instance of the type; otherwise,
        ///     <c>null</c>.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Instance method is more convenient for use from Lua.")]
        public object As( object @object, Type type )
        {
            return type.IsInstanceOfType(@object) ? @object : null;
        }

        /// <summary>
        /// Helper class for type-converting casts.
        /// </summary>
        /// <seealso cref="Cast"/>
        public static class CastHelper
        {
            /// <summary>
            /// Casts a double to a byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Byte Byte( Double value )
            {
                return (Byte)value;
            }

            /// <summary>
            /// Casts an integer to a byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Byte Byte( Int64 value )
            {
                return (Byte)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Byte Byte( UInt64 value )
            {
                return (Byte)value;
            }

            /// <summary>
            /// Casts a double to a signed byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static SByte SByte( Double value )
            {
                return (SByte)value;
            }

            /// <summary>
            /// Casts an integer to a signed byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static SByte SByte( Int64 value )
            {
                return (SByte)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a signed byte.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static SByte SByte( UInt64 value )
            {
                return (SByte)value;
            }

            /// <summary>
            /// Casts a double to a character.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Char Char( Double value )
            {
                return (Char)value;
            }

            /// <summary>
            /// Casts a single-character string to a character.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            /// <exception cref="ArgumentException"><paramref name="value"/> is not one character.</exception>
            public static Char Char( String value )
            {
                if (value.Length != 1)
                    throw new ArgumentException("Must be one character", "value");

                return value[0];
            }

            /// <summary>
            /// Casts an integer to a character.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Char Char( Int64 value )
            {
                return (Char)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a character.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Char Char( UInt64 value )
            {
                return (Char)value;
            }

            /// <summary>
            /// Casts a double to a 16-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int16 Int16( Double value )
            {
                return (Int16)value;
            }

            /// <summary>
            /// Casts an integer to a 16-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int16 Int16( Int64 value )
            {
                return (Int16)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 16-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Int16 Int16( UInt64 value )
            {
                return (Int16)value;
            }

            /// <summary>
            /// Casts a double to a 16-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt16 UInt16( Double value )
            {
                return (UInt16)value;
            }

            /// <summary>
            /// Casts an integer to a 16-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt16 UInt16( Int64 value )
            {
                return (UInt16)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 16-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt16 UInt16( UInt64 value )
            {
                return (UInt16)value;
            }

            /// <summary>
            /// Casts a double to a 32-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int32 Int32( Double value )
            {
                return (Int32)value;
            }

            /// <summary>
            /// Casts an integer to a 32-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int32 Int32( Int64 value )
            {
                return (Int32)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 32-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Int32 Int32( UInt64 value )
            {
                return (Int32)value;
            }

            /// <summary>
            /// Casts a double to a 32-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt32 UInt32( Double value )
            {
                return (UInt32)value;
            }

            /// <summary>
            /// Casts an integer to a 32-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt32 UInt32( Int64 value )
            {
                return (UInt32)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 32-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt32 UInt32( UInt64 value )
            {
                return (UInt32)value;
            }

            /// <summary>
            /// Casts a double to a 64-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int64 Int64( Double value )
            {
                return (Int64)value;
            }

            /// <summary>
            /// Casts an integer to a 64-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Int64 Int64( Int64 value )
            {
                return (Int64)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 64-bit integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Int64 Int64( UInt64 value )
            {
                return (Int64)value;
            }

            /// <summary>
            /// Casts a double to a 64-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt64 UInt64( Double value )
            {
                return (UInt64)value;
            }

            /// <summary>
            /// Casts an integer to a 64-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt64 UInt64( Int64 value )
            {
                return (UInt64)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a 64-bit unsigned integer.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static UInt64 UInt64( UInt64 value )
            {
                return (UInt64)value;
            }

            /// <summary>
            /// Casts a double to a single.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Single Single( Double value )
            {
                return (Single)value;
            }

            /// <summary>
            /// Casts an integer to a single.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Single Single( Int64 value )
            {
                return (Single)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a single.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Single Single( UInt64 value )
            {
                return (Single)value;
            }

            /// <summary>
            /// Casts a double to a double.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Double Double( Double value )
            {
                return value;
            }

            /// <summary>
            /// Casts an integer to a double.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            public static Double Double( Int64 value )
            {
                return (Double)value;
            }

            /// <summary>
            /// Casts an unsigned integer to a double.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The cast value.</returns>
            [CLSCompliant(false)]
            public static Double Double( UInt64 value )
            {
                return (Double)value;
            }

            // TODO: mechanism for implicit and explicit conversions
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Constructs a delegate for a Lua function.
        /// </summary>
        /// <param name="delegateType">The type of delegate to construct.</param>
        /// <param name="function">The Lua function.</param>
        /// <returns>The delegate for the Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegateType"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Instance method is more convenient for use from Lua.")]
        public Delegate NewDelegate( Type delegateType, LuaFunctionBase function )
        {
            if (delegateType == null)
                throw new ArgumentNullException("delegateType");
            if (function == null)
                throw new ArgumentNullException("function");

            return function.ToDelegate(delegateType);
        }

        /// <summary>
        /// Constructs a delegate for a method from a CLI method group.
        /// </summary>
        /// <param name="delegateType">The type of delegate to construct.</param>
        /// <param name="methodGroup">The userdata that represents a method group.</param>
        /// <returns>The delegate for the applicable method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegateType"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="methodGroup"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="methodGroup"/> does not represent a method
        ///     group.</exception>
        /// <exception cref="AmbiguousMatchException">Multiple methods in the method group are applicable
        ///     for creating a delegate of type <paramref name="delegateType"/>.</exception>
        /// <exception cref="MissingMethodException">No method in the method group is applicable for
        ///     creating a delegate of type <paramref name="delegateType"/>.</exception>
        [SecuritySafeCritical]
        public Delegate NewDelegate( Type delegateType, LuaUserData methodGroup )
        {
            if (delegateType == null)
                throw new ArgumentNullException("delegateType");
            if (methodGroup == null)
                throw new ArgumentNullException("methodGroup");

            Type type;
            string name;
            MethodInfo[] methods;
            object self;

            if (!_objectTranslator.TryGetMethodGroup(methodGroup, out type, out name, out methods, out self))
                throw new ArgumentException("Must represent a method group", "methodGroup");

            return NewDelegate(delegateType, type, name, methods, self);
        }

        private static Delegate NewDelegate( Type delegateType, Type type, string name, MethodInfo[] methods, object self )
        {
            LuaBinder binder = LuaBinder.Instance;

            MethodInfo method;

            try
            {
                if (methods.Length == 0)
                    throw new MissingMethodException();

                method = binder.SelectMethodForDelegate(0, methods, delegateType);

                if (method == null)
                    throw new MissingMethodException();
            }
            catch (AmbiguousMatchException)
            {
                throw new AmbiguousMatchException(String.Format("'{1}' designates ambiguous members of type '{0}' that can be used to create a delegate of type '{2}'", type, name, delegateType.ToString()));
            }
            catch (MissingMethodException)
            {
                throw new MissingMethodException(String.Format("'{1}' is not a member of type '{0}' that can be used to create a delegate of type '{2}'", type, name, delegateType.ToString()));
            }

            return Delegate.CreateDelegate(delegateType, self, method);
        }

        #endregion

        #region Events

        /// <summary>
        /// Adds a handler to an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="function">The Lua handler function.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not add-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void AddHandler( LuaUserData @event, LuaFunctionBase function )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (function == null)
                throw new ArgumentNullException("function");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            var @delegate = events.Length != 1 ? null : NewDelegate(events[0].EventHandlerType, function);

            AddHandler(type, name, events, self, @delegate);
        }

        /// <summary>
        /// Adds a handler to an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="methodGroup">The userdata that represents a method group.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="methodGroup"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="methodGroup"/> does not represent a method
        ///     group.</exception>
        /// <exception cref="AmbiguousMatchException">Multiple methods in the method group are applicable
        ///     for creating a delegate to handle <paramref name="event"/>.</exception>
        /// <exception cref="MissingMethodException">No method in the method group is applicable for
        ///     creating a delegate to handle <paramref name="event"/>.</exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not add-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void AddHandler( LuaUserData @event, LuaUserData methodGroup )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (methodGroup == null)
                throw new ArgumentNullException("methodGroup");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            var @delegate = events.Length != 1 ? null : NewDelegate(events[0].EventHandlerType, methodGroup);

            AddHandler(type, name, events, self, @delegate);
        }

        /// <summary>
        /// Adds a handler to an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="delegate">The delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="delegate"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="InvalidCastException"><paramref name="delegate"/> is not applicable to handle
        ///     <paramref name="event"/>.</exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not add-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void AddHandler( LuaUserData @event, Delegate @delegate )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (@delegate == null)
                throw new ArgumentNullException("delegate");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            AddHandler(type, name, events, self, @delegate);
        }

        private static void AddHandler( Type type, string name, EventInfo[] events, object self, Delegate @delegate )
        {
            if (events.Length == 0)
                throw new MissingMemberException(String.Format("'{1}' is not an event member of type '{0}'", type, name));
            if (events.Length != 1)
                throw new AmbiguousMatchException(String.Format("'{1}' designates ambiguous members of type '{0}'", type, name));

            EventInfo @event = events[0];

            MethodInfo addMethod = @event.GetAddMethod(nonPublic: false);
            if (addMethod == null)
                throw new MethodAccessException(String.Format("'{0}.{1}' is not add-accessible", type, name));

            if (!@event.EventHandlerType.IsAssignableFrom(@delegate.GetType()))
                throw new InvalidCastException(String.Format("Delegate type '{2}' is not applicable to handle '{0}.{1}'", type, name, @delegate.GetType()));

            addMethod.Invoke(self, new[] { @delegate });
        }

        /// <summary>
        /// Remove a handler from an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="function">The Lua handler function.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not remove-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void RemoveHandler( LuaUserData @event, LuaFunctionBase function )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (function == null)
                throw new ArgumentNullException("function");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            var @delegate = events.Length != 1 ? null : NewDelegate(events[0].EventHandlerType, function);

            RemoveHandler(type, name, events, self, @delegate);
        }

        /// <summary>
        /// Removes a handler from an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="methodGroup">The userdata that represents a method group.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="methodGroup"/> is <c>null</c>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="methodGroup"/> does not represent a method
        ///     group.</exception>
        /// <exception cref="AmbiguousMatchException">Multiple methods in the method group are applicable
        ///     for creating a delegate to handle <paramref name="event"/>.</exception>
        /// <exception cref="MissingMethodException">No method in the method group is applicable for
        ///     creating a delegate to handle <paramref name="event"/>.</exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not remove-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void RemoveHandler( LuaUserData @event, LuaUserData methodGroup )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (methodGroup == null)
                throw new ArgumentNullException("methodGroup");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            var @delegate = events.Length != 1 ? null : NewDelegate(events[0].EventHandlerType, methodGroup);

            RemoveHandler(type, name, events, self, @delegate);
        }

        /// <summary>
        /// Removes a handler from an event member.
        /// </summary>
        /// <param name="event">The userdata that represents an event member.</param>
        /// <param name="delegate">The delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="delegate"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="event"/> does not represent a event member.
        ///     </exception>
        /// <exception cref="InvalidCastException"><paramref name="delegate"/> is not applicable to handle
        ///     <paramref name="event"/>.</exception>
        /// <exception cref="MethodAccessException"><paramref name="event"/> is not remove-accessible.
        ///     </exception>
        [SecuritySafeCritical]
        public void RemoveHandler( LuaUserData @event, Delegate @delegate )
        {
            if (@event == null)
                throw new ArgumentNullException("event");
            if (@delegate == null)
                throw new ArgumentNullException("delegate");

            Type type;
            string name;
            EventInfo[] events;
            object self;

            if (!_objectTranslator.TryGetEvent(@event, out type, out name, out events, out self))
                throw new ArgumentException("Must represent an event", "event");

            RemoveHandler(type, name, events, self, @delegate);
        }

        private static void RemoveHandler( Type type, string name, EventInfo[] events, object self, Delegate @delegate )
        {
            if (events.Length == 0)
                throw new MissingMemberException(String.Format("'{1}' is not an event member of type '{0}'", type, name));
            if (events.Length != 1)
                throw new AmbiguousMatchException(String.Format("'{1}' designates ambiguous members of type '{0}'", type, name));

            EventInfo @event = events[0];

            if (!@event.EventHandlerType.IsAssignableFrom(@delegate.GetType()))
                throw new InvalidCastException(String.Format("Delegate type '{2}' is not applicable to handle '{0}.{1}'", type, name, @delegate.GetType()));

            MethodInfo removeMethod = @event.GetRemoveMethod(nonPublic: false);
            if (removeMethod == null)
                throw new MethodAccessException(String.Format("'{0}.{1}' is not remove-accessible", type, name));

            removeMethod.Invoke(self, new[] { @delegate });
        }

        #endregion
    }
}
