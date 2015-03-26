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
    /// Helper functions for arrays.
    /// </summary>
    internal static class ArrayUtility
    {
        /// <summary>
        /// Appends a specified array to another specified array.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array that will be appended to.</param>
        /// <param name="tail">The array that will be appended.</param>
        public static void Append<T>( ref T[] array, T[] tail )
        {
            int arrayLength = array.Length;
            int tailLength = tail.Length;
            Array.Resize(ref array, arrayLength + tailLength);
            Array.Copy(tail, 0, array, arrayLength, tail.Length);
        }
    }
}
