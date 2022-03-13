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
    /// <see cref="RightSubscribeMessage"/> allows subscribing messages send to a <see cref="database"/>.<br/>
    /// Allow subscribing a specific message by using explicit message <see cref="names"/>. E.g. 'std.Echo' <br/>
    /// Allow subscribing a group of messages by using a prefix. E.g. 'std.*' <br/>
    /// Allow subscribing all messages by using: '*'<br/>
    /// <br/>
    /// Note: commands are messages - so permission of subscribing commands is same as for messages.  
    /// </summary>
    public sealed class RightSubscribeMessage : Right
    {
                        public  string          database;
        [Fri.Required]  public  List<string>    names;
        
        public  override        RightType       RightType => RightType.subscribeMessage;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0], database);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message, database));
            }
            return new AuthorizeAny(list);
        }
    }
}