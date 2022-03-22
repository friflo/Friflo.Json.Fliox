// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

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
                        public  string          database;
        /// <summary>a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Fri.Required]  public  List<string>    names;
        
        public  override        RightType       RightType => RightType.subscribeMessage;
        
        public override IAuthorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0], database);
            }
            var list = new List<IAuthorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message, database));
            }
            return new AuthorizeAny(list);
        }
    }
}