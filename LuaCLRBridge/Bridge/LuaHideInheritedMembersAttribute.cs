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
    using System.Reflection;

    /// <summary>
    /// Specifies the visibility of the inherited members of a type when accessed from Lua.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class LuaHideInheritedMembersAttribute : Attribute
    {
        /// <summary>
        /// Determines whether the <see cref="LuaHideInheritedMembersAttribute"/> attribute is applied to a
        /// type.
        /// </summary>
        /// <param name="type">An object that describes the type.</param>
        /// <returns><c>true</c> if the attribute is applied to the type; otherwise, <c>false</c>.</returns>
        public static bool IsDefinedOn( MemberInfo type )
        {
            return Attribute.IsDefined(type, typeof(LuaHideInheritedMembersAttribute), false);
        }
    }
}
