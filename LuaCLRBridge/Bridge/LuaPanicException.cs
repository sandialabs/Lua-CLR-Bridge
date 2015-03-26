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
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a Lua panic-error message.
    /// </summary>
    [Serializable]
    public class LuaPanicException : LuaRuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaPanicException"/> class.
        /// </summary>
        public LuaPanicException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaPanicException"/> class with a specified
        /// error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public LuaPanicException( string message )
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaPanicException"/> class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public LuaPanicException( string message, Exception innerException )
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaPanicException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected LuaPanicException( SerializationInfo info, StreamingContext context )
            : base(info, context)
        {
        }
    }
}
