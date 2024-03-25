// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal static class ClientStatic {
        internal static readonly QueryPath   RefQueryPath = new RefQueryPath();
        internal const           bool        DefaultWritePretty   = false;
        internal const           bool        DefaultWriteNull     = false;
    }
}