// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class AuthUser {
        internal readonly   string              token;
        internal readonly   Authorizer          authorizer;
        internal readonly   HashSet<JsonKey>    clients = new HashSet<JsonKey>(JsonKey.Equality);
        
        internal AuthUser (string token, Authorizer authorizer) {
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
}