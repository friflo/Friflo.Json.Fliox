// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Host.FilterEntityResult;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    [Flags]
    public enum FilterEntityResult
    {
        HasMore         = 1,
        ReachedLimit    = 2,
        ReachedMaxCount = 3,  // caller need to add a cursor to the result
        FilterError     = 4
    }
    
    /// Default implementation. Performs a full table scan! Act as reference and is okay for small data sets
    public sealed class EntityFilterContext : IDisposable
    {
        public              List<EntityValue>       Result              => result;
        
        private  readonly   Pooled<JsonEvaluator>   poolJsonEvaluator;
        private  readonly   JsonEvaluator           evaluator;
        private  readonly   JsonFilter              jsonFilter;
        private  readonly   List<EntityValue>       result;
        private             string                  error;
        
        private  readonly   EntityContainer         container;
        private  readonly   long                    limit;
        private  readonly   long                    maxCount;
        
        public EntityFilterContext (QueryEntities command, EntityContainer container, SyncContext syncContext) {
            poolJsonEvaluator   = syncContext.pool.JsonEvaluator.Get();
            evaluator           = poolJsonEvaluator.instance;
            jsonFilter          = new JsonFilter(command.filterContext); // filter can be reused
            result              = new List<EntityValue>();
            limit               = command.limit     ?? long.MaxValue;
            maxCount            = command.maxCount  ?? long.MaxValue;
            this.container      = container;
        }
        
        public FilterEntityResult FilterEntity(in JsonKey key, in JsonValue json) {
            if (json.IsNull()) {
                return HasMore;
            }
            var match = evaluator.Filter(json, jsonFilter, out string filterError);
            
            if (filterError != null) {
                error = $"at {container.name}[{key}] {filterError}";
                return FilterError;
            }
            if (!match) {
                return HasMore;
            }
            result.Add(new EntityValue(key, json));
            if (result.Count >= limit) {
                return ReachedLimit;
            }
            if (result.Count >= maxCount) {
                return ReachedMaxCount;
            }
            return HasMore;
        }
        
        public QueryEntitiesResult QueryError(QueryEntitiesResult result) {
            result.Error = new TaskExecuteError (TaskErrorType.FilterError, error );
            return result;
        } 
        
        public void Dispose() {
            poolJsonEvaluator.Dispose();
        }
    }
}