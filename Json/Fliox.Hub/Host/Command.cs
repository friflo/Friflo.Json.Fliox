// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public readonly struct Command<TValue>{
        public              string          Name    { get; }
        public              IPool           Pool       => messageContext.pool;
        public              FlioxHub        Hub         => messageContext.hub;
        public              JsonValue       JsonValue   => json;
        
        private  readonly   JsonValue       json;
        private  readonly   MessageContext  messageContext;

        public   override   string          ToString() => Name;
        
        public              TValue          Value { get {
            using (var pooledMapper = messageContext.pool.ObjectMapper.Get()) {
                return pooledMapper.instance.Read<TValue>(json);
            }
        }}

        internal Command(string name, JsonValue json, MessageContext messageContext) {
            Name                = name;
            this.json           = json;  
            this.messageContext = messageContext;
        }
    }
}