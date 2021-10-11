// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class AuthUser {
        internal readonly   JsonKey             userId;
        internal readonly   string              token;
        internal readonly   Authorizer          authorizer;
        internal readonly   HashSet<JsonKey>    clients = new HashSet<JsonKey>(JsonKey.Equality);
        internal            int                 requests;
        internal            int                 tasks;

        public   override   string              ToString() => userId.AsString();

        internal AuthUser (in JsonKey userId, string token, Authorizer authorizer) {
            this.userId     = userId;
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
}