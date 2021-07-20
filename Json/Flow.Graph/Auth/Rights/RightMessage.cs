// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Flow.Auth.Rights
{
    public class RightMessage : Right
    {
        [Fri.Property(Required = true)]
        public          List<string>            names;
        public override RightType               RightType => RightType.message;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeMessage(names[0]);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeMessage(message));
            }
            return new AuthorizeAny(list);
        }
    }
}