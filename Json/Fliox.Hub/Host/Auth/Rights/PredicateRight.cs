// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Req = Friflo.Json.Fliox.RequiredFieldAttribute;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    public sealed class PredicateRight : Right
    {
        /// <summary>a specific predicate: 'TestPredicate', multiple predicates by prefix: 'Test*', all predicates: '*'</summary>
        [Req]   public              List<string>    names;
        
                public  override    RightType       RightType => RightType.predicate;
                public  override    Authorizer      ToAuthorizer() => throw new NotImplementedException();
                
        internal override void Validate(in RoleValidation validation) {
            foreach (var predicateName in names) {
                if (validation.predicates.Contains(predicateName))
                    continue;
                var error = $"unknown predicate: '{predicateName}' in role: {validation.role.id}";
                validation.errors.Add(error);
            }
        }

    }
    
    // ReSharper disable InconsistentNaming
    public enum RightType {
        allow,
        task,
        message,
        subscribeMessage,
        operation,
        predicate
    }
}