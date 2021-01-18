// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Mapper.Utils
{
    // FrifloIOException
    public class FrifloException : Exception
    {
        public FrifloException()
        :
            base("FrifloException") {
        }

        public FrifloException(String message)
        :
            base (message) {
        }

        public FrifloException(String message, Exception cause)
        :
            base (message, cause) {
        }





    }
}
