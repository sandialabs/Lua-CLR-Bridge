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
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// An equality comparer that considers objects equal iff they are the same object.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    public class IdentityEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        /// <summary>
        /// Determines whether two objects are the same.
        /// </summary>
        /// <param name="x">The first object.</param>
        /// <param name="y">The second object.</param>
        /// <returns><c>true</c> if the objects are the same; otherwise, <c>false</c>.</returns>
        public bool Equals( T x, T y )
        {
            return RuntimeHelpers.ReferenceEquals(x, y);
        }

        /// <summary>
        /// Returns the hash code of the object such that an object will always return the same hash code.
        /// </summary>
        /// <param name="obj">the object.</param>
        /// <returns>The hash code of the object.</returns>
        public int GetHashCode( T obj )
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
