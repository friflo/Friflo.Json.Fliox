// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public readonly struct EventTargetUser {
        public  readonly    JsonKey     user;
        public  readonly    JsonKey     client;
        
        public EventTargetUser (string user, string client = null) {
            this.user   = new JsonKey(user);
            this.client = new JsonKey(client);
        }
        
        public EventTargetUser (JsonKey user, JsonKey client = default) {
            this.user   = user;
            this.client = client;
        }
    }
}