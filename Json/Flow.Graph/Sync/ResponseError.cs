// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Flow.Sync
{
    public class ResponseError : DatabaseResponse
    {
        public          string  message;
        
        internal override   RequestType  requestType => RequestType.error;

        public override string  ToString() => message;

        public static StringBuilder ErrorFromException(Exception e) {
            var sb = new StringBuilder();
            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.Append(e.Message);
            return sb;
        }
    }
}