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

    /// <summary>
    /// Helper extension methods for <see cref="Type"/>.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether a type is a nullable value type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type is a nullable value type; otherwise, <c>false</c>.
        ///     </returns>
        internal static bool IsNullable( this Type type )
        {
            return type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Gets a value indicating whether a type is a delegate type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type is a delegate type; otherwise, <c>false</c>.
        ///     </returns>
        internal static bool IsDelegate( this Type type )
        {
            return typeof(Delegate).IsAssignableFrom(type) &&
                type != typeof(Delegate) &&
                type != typeof(MulticastDelegate);
        }
    }
}
