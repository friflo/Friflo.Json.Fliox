// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

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