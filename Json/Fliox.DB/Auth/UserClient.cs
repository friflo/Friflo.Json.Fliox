// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class UserClient {
        internal readonly   JsonKey                             userId;
        internal readonly   Dictionary<string, RequestStats>    stats = new Dictionary<string, RequestStats>();
        
        public   override   string                              ToString() => userId.AsString();

        internal UserClient (in JsonKey userId) {
            this.userId     = userId;
        }
    }
}