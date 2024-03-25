// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Cluster;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    internal readonly struct UserClient {
        internal readonly   ShortString                             userId;
        /// <b>Note</b> requires lock when accessing. Did not use ConcurrentDictionary to avoid heap allocation
        internal readonly   Dictionary<ShortString, RequestCount>   requestCounts;
        
        public   override   string                                  ToString() => userId.AsString();

        internal UserClient (in ShortString userId) {
            requestCounts   = new Dictionary<ShortString, RequestCount>(ShortString.Equality);
            this.userId     = userId;
        }
    }
}