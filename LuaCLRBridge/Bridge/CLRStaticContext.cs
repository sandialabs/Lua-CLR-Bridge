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
    /// Represents a CLI-type static context in Lua.
    /// </summary>
    [Serializable]
    public class CLRStaticContext
    {
        private readonly Type _contextType;

        /// <summary>
        /// Initializes a new instance of the <see cref="CLRStaticContext"/> class for a specified type.
        /// </summary>
        /// <param name="type">The type that will be represented.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public CLRStaticContext( Type type )
        {
            if (type == null)
                throw new ArgumentNullException("type");

            _contextType = type;
        }

        /// <summary>
        /// Gets the type that is represented.
        /// </summary>
        public Type ContextType
        {
            get { return _contextType; }
        }
    }
}
