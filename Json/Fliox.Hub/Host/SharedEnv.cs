// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="SharedEnv"/> provide a set of shared resources.
    /// In particular a <see cref="TypeStore"/> and a <see cref="Pool"/>.
    /// The resources contained by a <see cref="SharedEnv"/> are designed for being reused to avoid expensive
    /// heap allocations when required.
    /// <br/>
    /// <see cref="SharedEnv"/> reference is passed as a parameter to every <see cref="FlioxHub"/> instance.
    /// If null it defaults to <see cref="DefaultSharedEnv.Instance"/>.
    /// If an application needs to control the lifecycle of all shared resources it needs to create its own
    /// <see cref="SharedEnv()"/> instance and pass it to the constructor for all <see cref="FlioxHub"/> instances it creates.
    /// <br/>
    /// Access to shared resources is thread safe.
    /// </summary>
    public class SharedEnv : IDisposable
    {
        public  virtual     TypeStore   TypeStore   { get; }
        public  virtual     Pool        Pool        { get; }

        public SharedEnv() {
            TypeStore   = new TypeStore();
            Pool        = new Pool(this);
        }

        public virtual void Dispose () {
            Pool.Dispose();
            TypeStore.Dispose();
        }
    }
    
    public sealed class DefaultSharedEnv : SharedEnv
    {
        public  override    TypeStore   TypeStore   => HostTypeStore.Get();
        public  override    Pool        Pool        { get; }

        public static readonly DefaultSharedEnv Instance = new DefaultSharedEnv();
        
        private DefaultSharedEnv() {
            Pool = new Pool(this);
        }
        
        public override void Dispose () {
            Pool.Dispose();
        }
    }
}