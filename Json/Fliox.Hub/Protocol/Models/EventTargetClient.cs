// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public readonly struct EventTargetClient {
        public  readonly    JsonKey     user;
        public  readonly    JsonKey     client;
        
        public EventTargetClient (string user, string client = null) {
            this.user   = new JsonKey(user);
            this.client = new JsonKey(client);
        }
        
        public EventTargetClient (JsonKey user, JsonKey client = default) {
            this.user   = user;
            this.client = client;
        }
    }
}