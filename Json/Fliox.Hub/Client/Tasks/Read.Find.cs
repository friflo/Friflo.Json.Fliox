// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>originally extended SyncFunction. Its members are now in <see cref="SyncTask"/> </summary>
    public abstract  class FindFunction<TKey, T> where T : class {
        [DebuggerBrowsable(Never)]
        internal            TaskState   findState;
        public   abstract   string      Details { get; }

        
        /// <summary>
        /// Is true in case task execution was successful. Otherwise false. If false <see cref="Error"/> property is set. 
        /// </summary>
        /// <exception cref="TaskNotSyncedException"></exception>
        public              bool        Success { get {
            if (findState.IsExecuted())
                return !findState.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires SyncTasks(). {Details}");
        }}
        
        /// <summary>The error caused the task failing. Return null if task was successful - <see cref="Success"/> == true</summary>
        public              TaskError   Error { get {
            if (findState.IsExecuted())
                return findState.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.Error requires SyncTasks(). {Details}");
        } }
        
        
        internal bool IsOk(string method, out Exception e) {
            if (findState.IsExecuted()) {
                if (!findState.Error.HasErrors) {
                    e = null;
                    return true;
                }
                e = new TaskResultException(findState.Error.TaskError);
                return false;
            }
            e = new TaskNotSyncedException($"{method} requires SyncTasks(). {Details}");
            return false;
        }
        
        internal abstract void SetFindResult(Dictionary<TKey, T> values, TaskError taskError, List<TKey> keyBuffer);
    }
    
    public sealed class Find<TKey, T> : FindFunction<TKey, T> where T : class
    {
        private  readonly   TKey        key;
        private             T           result;
        private             JsonValue   rawResult;

        public              T           Result      => IsOk("Find.Result",   out Exception e) ? result : throw e;
        public              JsonValue   RawResult   => IsOk("Find.RawResult",out Exception e) ? rawResult : throw e;
        public   override   string      Details     => $"Find<{typeof(T).Name}> (id: '{key}')";
        
        private static readonly KeyConverter<TKey> KeyConvert = KeyConverter.GetConverter<TKey>();
        
        internal Find(TKey key) {
            this.key     = key;
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, TaskError taskError, List<TKey> keyBuffer) {
            findState.Executed  = true;
            result              = values[key];
            rawResult           = default;
            var entityErrors    = taskError?.entityErrors;
            if (entityErrors == null) {
                return;
            }
            var id = KeyConvert.KeyToId(key);
            if (entityErrors.TryGetValue(id, out var error)) {
                var errorInfo = new TaskErrorInfo();
                errorInfo.AddEntityError(error);
                findState.SetError(errorInfo);
            }
        }
    }
    
    public sealed class FindRange<TKey, T> : FindFunction<TKey, T> where T : class
    {
        private  readonly   Dictionary<TKey, T>                 results;
        private             Dictionary<JsonKey, EntityValue>    entities;
        public              Dictionary<TKey, T>                 Result    => IsOk("FindRange.Result",   out Exception e) ? results : throw e;
        public              Dictionary<TKey, EntityValue>       RawResult => IsOk("FindRange.RawResult",out Exception e) ? GetRawResult() : throw e;
        public   override   string                              Details => $"FindRange<{typeof(T).Name}> (ids: {results.Count})";
        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();
        
        internal FindRange(ICollection<TKey> keys) {
            results = Set.CreateDictionary<TKey, T>(keys.Count);
            foreach (var key in keys) {
                results.TryAdd(key, null);
            }
        }
        
        private Dictionary<TKey, EntityValue> GetRawResult() {
            var rawResults = new Dictionary<TKey, EntityValue>(results.Count);
            foreach (var pair in results) {
                var key     = pair.Key;
                var id      = KeyConvert.KeyToId(key);
                var value   = entities[id];
                rawResults.Add(key, value);
            }
            return rawResults;
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, TaskError taskError, List<TKey> keyBuffer) {
            var errorInfo       = new TaskErrorInfo();
            var entityErrors    = taskError?.entityErrors;
            keyBuffer.Clear();
            foreach (var entry in results) {
                keyBuffer.Add(entry.Key);
            }
            foreach (var key in keyBuffer) { 
                results[key] = values[key];
                if (entityErrors == null) {
                    continue;
                }
                var id = KeyConvert.KeyToId(key);
                if (entityErrors.TryGetValue(id, out var error)) {
                    errorInfo.AddEntityError(error);
                }
            }
            if (errorInfo.HasErrors) {
                findState.SetError(errorInfo);
            }
            findState.Executed = true;
        }
    }
}