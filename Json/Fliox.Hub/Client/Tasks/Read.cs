// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
    public abstract  class FindTask<TKey, T> : SyncTask where T : class {
        internal                    TaskState   findState;
        
        public   abstract override  string      Details { get; }
        internal abstract override  TaskState   State   { get; }

        internal abstract void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> buf);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class Find<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   TKey        key;
        private             T           result;
        private             JsonValue   rawResult;

        public              T           Result      => IsOk("Find.Result",   out Exception e) ? result : throw e;
        public              JsonValue   RawResult   => IsOk("Find.RawResult",out Exception e) ? rawResult : throw e;
        internal override   TaskState   State       => findState;
        public   override   string      Details     => $"Find<{typeof(T).Name}> (id: '{key}')";
        
        private static readonly KeyConverter<TKey> KeyConvert = KeyConverter.GetConverter<TKey>();
        
        internal Find(TKey key) {
            this.key     = key;
        }
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> keysBuf) {
            TaskErrorInfo error = new TaskErrorInfo();
            var id          = KeyConvert.KeyToId(key);
            var value       = entities[id];
            var entityError = value.Error;
            if (entityError == null) {
                findState.Executed  = true;
                result              = values[key];
                rawResult           = value.Json;
                return;
            }
            error.AddEntityError(entityError);
            findState.SetError(error);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class FindRange<TKey, T> : FindTask<TKey, T> where T : class
    {
        private  readonly   Dictionary<TKey, T>                 results;
        private             Dictionary<JsonKey, EntityValue>    entities;
        public              Dictionary<TKey, T>                 Result    => IsOk("FindRange.Result",   out Exception e) ? results : throw e;
        public              Dictionary<TKey, EntityValue>       RawResult => IsOk("FindRange.RawResult",out Exception e) ? GetRawResult() : throw e;
        internal override   TaskState                           State   => findState;
        public   override   string                              Details => $"FindRange<{typeof(T).Name}> (#ids: {results.Count})";
        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();
        
        internal FindRange(ICollection<TKey> keys) {
            results = SyncSet.CreateDictionary<TKey, T>(keys.Count);
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
        
        internal override void SetFindResult(Dictionary<TKey, T> values, Dictionary<JsonKey, EntityValue> entities, List<TKey> keysBuf) {
            this.entities = entities; 
            TaskErrorInfo error = new TaskErrorInfo();
            keysBuf.Clear();
            foreach (var result in results) {
                var key = result.Key;
                var id  = KeyConvert.KeyToId(key);
                var entityError = entities[id].Error;
                if (entityError == null) {
                    keysBuf.Add(key);
                } else {
                    error.AddEntityError(entityError);
                }
            }
            foreach (var key in keysBuf) {
                results[key] = values[key];
            }
            if (error.HasErrors) {
                findState.SetError(error);
                return;
            }
            findState.Executed = true;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadTask<TKey, T> : SyncTask, IReadRefsTask<T> where T : class
    {
        internal            TaskState               state;
        private  readonly   EntitySet<TKey, T>      set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<TKey, T>     result      = SyncSet.CreateDictionary<TKey,T>();
        internal readonly   List<FindTask<TKey, T>> findTasks   = new List<FindTask<TKey, T>>();

        public              Dictionary<TKey, T>     Result      => IsOk("ReadTask.Result", out Exception e) ? result      : throw e;

        internal override   TaskState               State       => state;
        public   override   string                  Details     => $"ReadTask<{typeof(T).Name}> (#ids: {result.Count})";
        

        internal ReadTask(EntitySet<TKey, T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<TKey, T> Find(TKey key) {
            if (key == null)
                throw new ArgumentException($"ReadTask.Find() id must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.Add(key, null);
            var find = new Find<TKey, T>(key);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        public FindRange<TKey, T> FindRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsExecuted())
                throw AlreadySyncedError();
            result.EnsureCapacity(result.Count + keys.Count);
            foreach (var id in keys) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                result.TryAdd(id, null);
            }
            var find = new FindRange<TKey, T>(keys);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        
        // --- Relation
        public ReadRefTask<TRef> ReadRelation<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(relation, path);
        }
        
        public ReadRefTask<TRef> ReadRelation<TRefKey, TRef> (EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class   where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TRef>(relation, path);
        }
        
        public ReadRefTask<TRef> ReadRelation<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return ReadRefByPath<TRef>(relation, selector.path);
        }
        
        
        // --- IReadRefsTask<T>
        public ReadRefsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, set.intern.store);
        }
        
        public ReadRefsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, set.intern.store);
        }
       
        public ReadRefsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, set.intern.store);
        }
        
        public ReadRefsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class  where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, set.intern.store);
        }
        
        public ReadRefsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(relation, selector.path, set.intern.store);
        }


        // lab - ReadRefs by Entity Type
        /*
        public ReadRefsTask<TRef> ReadRefsOfType<TRef>() where TRef : class {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<object> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        } */
        
        private ReadRefTask<TRef> ReadRefByPath<TRef>(EntitySet relation, string path) where TRef : class {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TRef>)subRefsTask;
            // var relation = set.intern.store._intern.GetSetByType(typeof(TRef));
            var keyName     = relation.GetKeyName();
            var isIntKey    = relation.IsIntKey();
            var newQueryRefs = new ReadRefTask<TRef>(this, path, relation.name, keyName, isIntKey, set.intern.store);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            set.intern.store.AddTask(newQueryRefs);
            return newQueryRefs;
        }
        
        internal override void AddFailedTask(List<SyncTask> failed) {
            /*foreach (var findTask in findTasks) {
                if (findTask.State.Error.HasErrors) {
                    failed.Add(findTask);
                }
            }*/
        }
    }
}
