// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- response -----------------------------------
    public class ErrorResponse : ProtocolResponse
    {
        public              string      message;
        
        internal override   MessageType MessageType => MessageType.error;

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