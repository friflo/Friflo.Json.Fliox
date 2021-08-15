// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Flow.Auth.Rights
{
    public class RightSubscribeMessage : Right
    {
        [Fri.Property(Required = true)]
        public              List<string>    names;
        public  override    RightType       RightType => RightType.subscribeMessage;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0]);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message));
            }
            return new AuthorizeAny(list);
        }
    }
}