// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.UserAuth;

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
    public sealed class SubscribeMessageRight : TaskRight
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Required]  public  string          database;
        /// <summary>a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Required]  public  List<string>    names;
        
        public  override    RightType       RightType => RightType.subscribeMessage;
        
        public override TaskAuthorizer ToAuthorizer() {
            var databaseName = database;
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0], databaseName);
            }
            var list = new List<TaskAuthorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message, databaseName));
            }
            return new AuthorizeAny(list);
        }
        
        internal override void Validate(in RoleValidation validation) {
            validation.ValidateDatabase(this, database);
        }

    }
}