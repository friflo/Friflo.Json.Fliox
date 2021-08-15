// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Flow.Auth.Rights
{
    public class RightPredicate : Right
    {
        [Fri.Property(Required = true)]
        public              List<string>    names;
        public  override    RightType       RightType => RightType.predicate;
        
        public  override    Authorizer      ToAuthorizer() => throw new NotImplementedException();
        
    }
    
    // ReSharper disable InconsistentNaming
    public enum RightType {
        allow,
        task,
        message,
        subscribeMessage,
        database,
        predicate
    }
}