// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="Command{TParam}"/> contains all data relevant for command execution as properties. <br/>
    /// - the command <see cref="Name"/> == method name <br/>
    /// - the input parameter <see cref="Param"/> of type <typeparamref name="TParam"/> <br/>
    /// - the input parameter <see cref="JsonParam"/> as raw JSON <br/>
    /// - the <see cref="DatabaseName"/> <br/>
    /// - the <see cref="Database"/> instance <br/>
    /// - the <see cref="Hub"/> exposing general Hub information <br/>
    /// - a <see cref="Pool"/> mainly providing common utilities to transform JSON <br/> 
    /// </summary>
    /// <typeparam name="TParam">Type of the command input parameter</typeparam>
    public readonly struct Command<TParam>{
        public              string          Name            { get; }
        public              IPool           Pool            => messageContext.pool;
        public              FlioxHub        Hub             => messageContext.hub;
        public              JsonValue       JsonParam       => param;
        public              string          DatabaseName    => messageContext.DatabaseName;
        public              EntityDatabase  Database        => messageContext.Database;
        public              User            User            => messageContext.User;
        public              JsonKey         ClientId        => messageContext.clientId;
        
        internal            MessageContext  MessageContext  => messageContext;
        
        private  readonly   JsonValue       param;
        private  readonly   MessageContext  messageContext;

        public   override   string          ToString()      => Name;
        
        public              TParam          Param { get {
            using (var pooledMapper = messageContext.pool.ObjectMapper.Get()) {
                return pooledMapper.instance.Read<TParam>(param);
            }
        }}
        
        public              UserInfo        UserInfo { get {
            var user = messageContext.User;
            return new UserInfo (user.userId, user.token, messageContext.clientId);
        } }


        internal Command(string name, JsonValue param, MessageContext messageContext) {
            Name                = name;
            this.param          = param;  
            this.messageContext = messageContext;
        }
    }
}