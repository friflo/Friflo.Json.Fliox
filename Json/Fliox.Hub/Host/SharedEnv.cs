// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public abstract class SharedEnv : IDisposable
    {
        public  abstract    TypeStore   TypeStore   { get; }
        public  abstract    Pool        Pool        { get; }
        
        public  abstract    void        Dispose();
    }
    
    public sealed class SharedAppEnv : SharedEnv
    {
        private readonly    TypeStore   typeStore;
        private readonly    Pool        pool;
        
        public  override    TypeStore   TypeStore   => typeStore;
        public  override    Pool        Pool        => pool;
        
        public SharedAppEnv() {
            typeStore   = new TypeStore();
            pool        = new Pool(this);
        }

        public override void Dispose () {
            pool.Dispose();
            typeStore.Dispose();
        }
    }
    
    public sealed class SharedHostEnv : SharedEnv
    {
        private readonly    Pool        pool;
        
        public  override    TypeStore   TypeStore   => HostTypeStore.Get();
        public  override    Pool        Pool        => pool;
        
        public static readonly SharedHostEnv Instance = new SharedHostEnv();
        
        private SharedHostEnv() {
            pool = new Pool(this);
        }
        
        public override void Dispose () {
            pool.Dispose();
        }
    }
}