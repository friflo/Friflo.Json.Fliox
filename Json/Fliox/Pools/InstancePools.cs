// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Pools
{
    /// <summary> A pool for class instances of reference types. </summary>
    /// <remarks> <see cref="InstancePools"/> is not thread safe </remarks>
    public sealed class InstancePools
    {
        internal            int                 version;
        internal  readonly  List<InstancePool>  pools       =  new List<InstancePool>();
        public    override  string              ToString()  => GetString();

        public InstancePools() { }

        public void Reuse() {
            version++;
        }
    
        private string GetString() {
            var used        = 0;
            var count       = 0;
            var typeCount   = 0;
            foreach (var pool in pools) {
                count       += pool.Count;
                typeCount++;
                if (version == pool.Version) {
                    used    += pool.Used;
                }
            }
            return $"count: {count}, used: {used}, types: {typeCount}, version: {version}";
        }
    }
}