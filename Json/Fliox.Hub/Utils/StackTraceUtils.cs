// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal static class StackTraceUtils
    {
        internal static string GetStackTrace(Exception e, bool fileInfo) {
            string stack;
            if (fileInfo) {
                stack   = e.StackTrace;                         // contains file info: source path & line
            } else {
                stack   = new StackTrace(e, false).ToString();  // stacktrace without file info: source path & line
                stack   = stack.Replace("\r\n", "\n");
                stack   = stack.Substring(0, stack.Length - 1); // remove trailing LF
            }
            
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0 || NETSTANDARD2_1
            if (stack != null) {
                // Unity, NETSTANDARD2_0 & NETSTANDARD2_1 add StackTrace sections starting with:
                // --- End of stack trace from previous location where exception was thrown ---
                // Remove these sections as they bloat the stacktrace assuming the relevant part of the stacktrace
                // is at the beginning.
                var endOfStackTraceFromPreviousLocation = stack.IndexOf("\n--- End of stack", StringComparison.Ordinal);
                if (endOfStackTraceFromPreviousLocation != -1) {
                    stack = stack.Substring(0, endOfStackTraceFromPreviousLocation);
                }
            }
#endif
            return stack;
        }
    }
}