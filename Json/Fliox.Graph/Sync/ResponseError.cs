// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Fliox.Sync
{
    // ----------------------------------- response -----------------------------------
    public class ErrorResponse : DatabaseResponse
    {
        public              string      message;
        
        internal override   RequestType RequestType => RequestType.error;

        public override     string      ToString() => message;

        public static StringBuilder ErrorFromException(Exception e) {
            var sb = new StringBuilder();
            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.Append(e.Message);
            return sb;
        }
    }
}