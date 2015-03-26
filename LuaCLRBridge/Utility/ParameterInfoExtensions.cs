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
    /// Helper extension methods for <see cref="ParameterInfo"/>.
    /// </summary>
    internal static class ParameterInfoExtensions
    {
        /// <summary>
        /// Gets a value indicating whether a parameter is a variable-argument parameter.
        /// </summary>
        /// <param name="parameterInfo">The parameter information.</param>
        /// <returns><c>true</c> if the parameter is a variable-argument parameter; otherwise, <c>false</c>.
        ///     </returns>
        internal static bool IsParams( this ParameterInfo parameterInfo )
        {
            return Attribute.IsDefined(parameterInfo, typeof(ParamArrayAttribute));
        }
    }
}
