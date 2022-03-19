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
    /// <see cref="RightSendMessage"/> allows sending messages to a <see cref="database"/> by a set of <see cref="names"/>.<br/>
    /// <br/>
    /// Note: commands are messages - so permission of sending commands is same as for messages.
    /// </summary>
    public sealed class RightSendMessage : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
                        public  string          database;
        /// <summary>a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Fri.Required]  public  List<string>    names;
        public  override        RightType       RightType => RightType.message;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeSendMessage(names[0], database);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSendMessage(message, database));
            }
            return new AuthorizeAny(list);
        }
    }
}