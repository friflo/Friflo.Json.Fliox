// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map
{
    public sealed class InvalidTypeException : Exception {
        public InvalidTypeException (string msg) : base(msg) { }
    }
}