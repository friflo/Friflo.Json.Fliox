// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public readonly struct Command<TParam>{
        public              string          Name            { get; }
        public              IPool           Pool            => messageContext.pool;
        public              FlioxHub        Hub             => messageContext.hub;
        public              JsonValue       JsonParam       => param;
        public              string          DatabaseName    => messageContext.DatabaseName;
        public              EntityDatabase  Database        => messageContext.Database;
        
        private  readonly   JsonValue       param;
        private  readonly   MessageContext  messageContext;

        public   override   string          ToString()      => Name;
        
        public              TParam          Param { get {
            using (var pooledMapper = messageContext.pool.ObjectMapper.Get()) {
                return pooledMapper.instance.Read<TParam>(param);
            }
        }}

        internal Command(string name, JsonValue param, MessageContext messageContext) {
            Name                = name;
            this.param          = param;  
            this.messageContext = messageContext;
        }
    }
}