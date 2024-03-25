// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="SharedEnv"/> provide a set of shared resources available via <see cref="FlioxHub.sharedEnv"/>.
    /// </summary>
    /// <remarks>
    /// In particular it provides a <see cref="TypeStore"/> and a <see cref="Pool"/>.
    /// The resources contained by a <see cref="SharedEnv"/> are designed for being reused to avoid expensive
    /// heap allocations when required.
    /// The intention is to use only a single <see cref="SharedEnv"/> instance within the whole application.
    /// <br/>
    /// <see cref="SharedEnv"/> references are passed as a parameter to every <see cref="FlioxHub"/> constructor.
    /// If null it defaults to the <see cref="Default"/> <see cref="SharedEnv"/> instance.
    /// If an application needs to control the lifecycle of all shared resources it needs to create its own
    /// <see cref="SharedEnv()"/> instance and pass it to the constructor to all <see cref="FlioxHub"/> instances it creates.
    /// <br/>
    /// Access to shared resources is thread safe.
    /// </remarks>
    public sealed class SharedEnv : IDisposable, ILogSource
    {
        // --- private / internal
                        private  readonly   string                      name;
        [Browse(Never)] internal readonly   TypeStore                   typeStore       = new TypeStore();
                        internal readonly   SharedCache                 sharedCache     = new SharedCache();
        [Browse(Never)] internal readonly   HubLogger                   hubLogger       = new HubLogger();
                        internal readonly   Pool                        pool;
        [Browse(Never)] internal readonly   HubTypes                    types;
        // --- public
                        public              TypeStore                   TypeStore       => typeStore;
                        public              ObjectPool<MemoryBuffer>    MemoryBuffer    => pool.MemoryBuffer;
                        public              IHubLogger                  Logger {
                            get => hubLogger.instance;
                            set => hubLogger.instance = value ?? throw new ArgumentNullException (nameof(Logger));
                        }
        public override                     string                      ToString() => name != null ? $"'{name}'" : base.ToString();

        private static readonly SharedEnv DefaultSharedEnv  = new SharedEnv("DefaultSharedEnv");
        /// <summary>Set breakpoint to check if <see cref="DefaultSharedEnv"/> is used </summary>
        public  static          SharedEnv Default           => DefaultSharedEnv;

        public SharedEnv() {
            types       = new HubTypes(typeStore);
            pool        = new Pool(typeStore);
        }
        
        public SharedEnv(string name) {
            this.name   = name;
            types       = new HubTypes(typeStore);
            pool        = new Pool(typeStore);
        }

        public void Dispose () {
            sharedCache.Dispose();
            pool.Dispose();
            typeStore.Dispose();
        }
        
        /// obsolete - TODO remove
        public void DisposePool () {
            pool.Dispose();
        }
    }
    
    internal readonly struct HubTypes
    {
        internal readonly  TypeMapper<ProtocolMessage>  protocol;
        
        internal HubTypes(TypeStore typeStore) {
            protocol    = typeStore.GetTypeMapper<ProtocolMessage>();   
        }
    } 
    
    /// <summary>
    /// <see cref="SharedCache"/> is a cache for shared instances directly or indirectly used by
    /// <see cref="HostCommandHandler{TParam,TResult}"/> or <see cref="SyncRequestTask"/> methods. <br/>
    /// Cached instances created and returned by <see cref="SharedCache"/> must me immutable to enable
    /// concurrent and / or parallel usage.
    /// </summary>
    internal sealed class SharedCache : IDisposable
    {
        private readonly    NativeValidationSet validationSet;
        
        internal SharedCache() {
            validationSet = new NativeValidationSet();
        }

        public void Dispose() {
            validationSet.Dispose();
        }

        /// <summary> Return an immutable <see cref="ValidationType"/> instance for the given <param name="type"></param></summary>
        public ValidationType GetValidationType(Type type) {
            return validationSet.GetValidationType(type);
        }
        
        internal void AddRootType(Type rootType) {
            validationSet.AddRootType(rootType);
        }
    }
}