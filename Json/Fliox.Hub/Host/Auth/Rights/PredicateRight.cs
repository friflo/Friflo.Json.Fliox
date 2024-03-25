// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.UserAuth;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    public sealed class PredicateRight : TaskRight
    {
        /// <summary>a specific predicate: 'TestPredicate', multiple predicates by prefix: 'Test*', all predicates: '*'</summary>
        [Required]  public              List<string>    names;
        
                    public  override    RightType       RightType => RightType.predicate;
                    public  override    TaskAuthorizer  ToAuthorizer() => throw new NotImplementedException();
                
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
        dbFull              = 1,
        dbTask              = 2,
        dbContainer         = 3,
        message             = 4,
        subscribeMessage    = 5,
        predicate           = 6
    }
}