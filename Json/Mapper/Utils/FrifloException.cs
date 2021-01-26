// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Mapper.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FrifloException : Exception
    {
        public FrifloException(String message) :
            base (message)
        {
        }

        public FrifloException(String message, Exception cause) :
            base (message, cause)
        {
        }
    }
}
