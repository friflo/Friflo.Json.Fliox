// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="SubscribeMessageRight"/> allows subscribing messages send to a <see cref="database"/>.<br/>
    /// <br/>
    /// Note: commands are messages - so permission of subscribing commands is same as for messages.  
    /// </summary>
    public sealed class SubscribeMessageRight : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Req]   public  string          database;
        /// <summary>a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Req]   public  List<string>    names;
        
                public  override        RightType       RightType => RightType.subscribeMessage;
        
        public override Authorizer ToAuthorizer() {
            var databaseName = database;
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0], databaseName);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message, databaseName));
            }
            return new AuthorizeAny(list);
        }
    }
}