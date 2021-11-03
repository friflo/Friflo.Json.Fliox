// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// Interface used to define shared resources used by <see cref="Client.FlioxClient"/> and <see cref="FlioxHub"/>.
    /// Access to these shared resources is thread safe.
    /// It is not an interface by intention to avoid coupling resource ownership with entire different topics.
    /// </summary>
    public abstract class SharedEnv : IDisposable
    {
        public  abstract    TypeStore   TypeStore   { get; }
        public  abstract    Pool        Pool        { get; }
        
        public  abstract    void        Dispose();
    }
    
    public sealed class SharedAppEnv : SharedEnv
    {
        public  override    TypeStore   TypeStore   { get; }
        public  override    Pool        Pool        { get; }

        public SharedAppEnv() {
            TypeStore   = new TypeStore();
            Pool        = new Pool(this);
        }

        public override void Dispose () {
            Pool.Dispose();
            TypeStore.Dispose();
        }
    }
    
    public sealed class SharedHostEnv : SharedEnv
    {
        public  override    TypeStore   TypeStore   => HostTypeStore.Get();
        public  override    Pool        Pool        { get; }

        public static readonly SharedHostEnv Instance = new SharedHostEnv();
        
        private SharedHostEnv() {
            Pool = new Pool(this);
        }
        
        public override void Dispose () {
            Pool.Dispose();
        }
    }
}