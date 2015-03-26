﻿/* LuaCLRBridge
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
    /// The exception that is thrown when a problem occurs while translating CLI objects for Lua.
    /// </summary>
    [Serializable]
    public class ObjectTranslatorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTranslatorException"/> class.
        /// </summary>
        public ObjectTranslatorException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTranslatorException"/> class with a specified
        /// error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ObjectTranslatorException( string message )
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTranslatorException"/> class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ObjectTranslatorException( string message, Exception innerException )
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTranslatorException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ObjectTranslatorException( SerializationInfo info, StreamingContext context )
            : base(info, context)
        {
        }
    }
}
