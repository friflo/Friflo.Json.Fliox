// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    public readonly struct Command<TValue> {
        public              string          Name    { get; }
        public              IPools          Pools   => messageContext.pools;
        public              FlioxHub        Hub     => messageContext.hub;
        
        private  readonly   JsonUtf8        json;
        private  readonly   MessageContext  messageContext;

        public   override   string          ToString() => Name;
        
        public              TValue          Value { get {
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                return pooledMapper.instance.Read<TValue>(json);
            }
        }}

        internal Command(string name, JsonUtf8 json, MessageContext messageContext) {
            Name                = name;
            this.json           = json;  
            this.messageContext = messageContext;
        }
    }
}