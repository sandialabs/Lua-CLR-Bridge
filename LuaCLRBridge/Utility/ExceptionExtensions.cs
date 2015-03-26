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
    using System.Security;
    using System.Security.Permissions;

    /// <summary>
    /// Helper extension methods for exceptions.
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Causes 'throw' to not trash the stack trace.
        /// </summary>
        /// <param name="ex">Exception to operate on.</param>
        [SecurityCritical]
        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        internal static void PreserveStackTrace( this Exception ex )
        {
            try
            {
                FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                FieldInfo stackTraceString = typeof(Exception).GetField("_stackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                FieldInfo stackTrace = typeof(Exception).GetField("_stackTrace", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                string remoteStackTraceStringValue = null;
                if (remoteStackTraceString != null)
                {
                    remoteStackTraceStringValue = remoteStackTraceString.GetValue(ex) as string;
                    remoteStackTraceString.SetValue(ex, String.Empty);

                    string tempStackTraceString = ex.StackTrace;
                    if (tempStackTraceString != null && tempStackTraceString.Length > 0)
                        remoteStackTraceStringValue += tempStackTraceString + Environment.NewLine;

                    remoteStackTraceString.SetValue(ex, remoteStackTraceStringValue);

                    if (stackTraceString != null)
                        stackTraceString.SetValue(ex, null);
                    if (stackTrace != null)
                        stackTrace.SetValue(ex, null);
                }
            }
            catch (FieldAccessException)
            {
            }
        }

        /// <summary>
        /// Appends a string to the preserved stack trace.
        /// </summary>
        /// <param name="ex">Exception to operate on.</param>
        /// <param name="stackTraceString">String to be appended.</param>
        [SecurityCritical]
        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        internal static void AppendPreservedStackTrace( this Exception ex, string stackTraceString )
        {
            try
            {
                FieldInfo remoteStackTraceString = typeof(Exception)
                    .GetField("_remoteStackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (remoteStackTraceString != null)
                    remoteStackTraceString.SetValue(ex, remoteStackTraceString.GetValue(ex) + stackTraceString);
            }
            catch (FieldAccessException)
            {
            }
        }
    }
}
