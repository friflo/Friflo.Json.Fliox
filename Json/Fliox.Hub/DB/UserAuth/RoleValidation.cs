// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    
    internal readonly struct RoleValidation {
        private   readonly  HashSet<string>     databases;
        internal  readonly  HashSet<string>     predicates;
        internal  readonly  List<string>        errors;
        internal  readonly  Role                role;
        
        
        internal RoleValidation (HashSet<string> databases, HashSet<string> predicates, List<string> errors) {
            this.databases  = databases;
            this.predicates = predicates;
            this.errors     = errors;
            this.role       = null;
        }
        
        internal RoleValidation (in RoleValidation validation, Role role) {
            this.databases  = validation.databases;
            this.predicates = validation.predicates;
            this.errors     = validation.errors;
            this.role       = role;
        }
        
        internal void ValidateDatabase(TaskRight taskRight, string database) {
            if (database == null) {
                var error = $"missing database in role: {role.id}, right: {taskRight.RightType}";
                errors.Add(error);
                return;
            }
            if (databases != null) {
                var isPrefix = database.EndsWith("*");
                if (!isPrefix && !databases.Contains(database)) {
                    var error = $"database not found: '{database}' in role: {role.id}";
                    errors.Add(error);
                    return;
                }
            }
        }
    }
}