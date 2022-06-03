// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client
{
    public class RefsPath<TEntity, TRefKey, TRef>   where TEntity : class
                                                    where TRef    : class 
    {
        public readonly string path;

        public override string ToString() => path;

        internal RefsPath(string path) {
            this.path = path;
        }
    }
    
    public sealed class RefPath<TEntity, TRefKey, TRef> : RefsPath<TEntity, TRefKey, TRef>
                                            where TEntity : class
                                            where TRef    : class 
    {
        internal RefPath(string path) : base (path) { }
    }
}