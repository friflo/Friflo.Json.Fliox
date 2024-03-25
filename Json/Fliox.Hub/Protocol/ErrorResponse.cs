// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Friflo.Json.Fliox.Hub.Utils;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- response -----------------------------------
    /// <summary>
    /// <see cref="ErrorResponse"/> is returned for a <see cref="SyncRequest"/> in case the whole requests failed
    /// </summary>
    public sealed class ErrorResponse : ProtocolResponse
    {
        /// <summary>error message</summary>
        public              string              message;
        /// <summary>error type: invalid request or execution exception</summary>
        public              ErrorResponseType   type;
        
        internal override   MessageType         MessageType => MessageType.error;

        public override     string              ToString() => message;

        public static StringBuilder ErrorFromException(Exception e) {
            var sb = new StringBuilder();
            sb.Append("Internal ");
            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.Append(e.Message);
            var stack = StackTraceUtils.GetStackTrace(e, true);
            if (stack != null) {
                // Remove StackTrace sections starting with:
                // --- End of stack trace from previous location where exception was thrown ---
                // Remove these sections as they bloat the stacktrace assuming the relevant part of the stacktrace
                // is at the beginning.
                var endOfStackTraceFromPreviousLocation = stack.IndexOf("\n--- End of stack", StringComparison.Ordinal);
                if (endOfStackTraceFromPreviousLocation != -1) {
                    stack = stack.Substring(0, endOfStackTraceFromPreviousLocation);
                }
                sb.Append('\n');
                sb.Append(stack);
                sb.Append(" --- Internal End");
            }
            return sb;
        }
    }
    
    public enum ErrorResponseType
    {
        /// <summary>Invalid JSON request or invalid request parameters. Maps to HTTP status code 400 (Bad Request)</summary>
        BadRequest  = 1,
        /// <summary>Internal exception. Maps to HTTP status code 500 (Internal Server Error)</summary>
        Exception   = 2,
        /// <summary>Invalid JSON response. Maps to HTTP status code 500 (Internal Server Error)</summary>
        BadResponse = 3
    }
}