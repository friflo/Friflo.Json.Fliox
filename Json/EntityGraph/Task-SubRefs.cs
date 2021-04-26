// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;


namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public interface ISubRefsTask
    {
        string                              Selector    { get; }
        string                              Container   { get; }
        Dictionary<string, ISubRefsTask>    SubRefs     { get; }
    }

    public class SubRefsTask<T> : RefsTask<T>, ISubRefsTask where T : Entity
    {
        private   readonly  ISetTask                            parent;
        internal  readonly  Dictionary<string, T>               results = new Dictionary<string, T>();
            
        public    override  string                              Label => $"{parent.Label} {Selector}";
        public    override  string                              ToString() => Label;
            
        public              string                              Selector  { get; }
        public              string                              Container { get; }
        public              Dictionary<string, ISubRefsTask>    SubRefs => subRefs;

        public              Dictionary<string, T>   Results          => synced ? results      : throw RequiresSyncError("QueryRefsTask.Results requires Sync().");
        public              T                       this[string id]  => synced ? results[id]  : throw RequiresSyncError("QueryRefsTask[] requires Sync().");

        internal SubRefsTask(ISetTask parent, string selector, string container)
        {
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
        }
    }
    
}