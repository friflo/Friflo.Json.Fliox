// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Pools
{
    /// <summary> Contain pooled instances of a specific type </summary>
    internal struct PoolIntern<T>
    {
        internal    T[]         objects;
        internal    int         used;
        internal    int         version;
        internal    int         count;

        public override string  ToString() => GetString();

        internal PoolIntern(T[] objects) {
            this.objects    = objects;
            used            =  0;
            count           =  0;
            version         = -1;
        }
        
        internal T Create(Func<T> factory) {
            used++;
            var instance        = factory();
            if (count < objects.Length) {
                objects[count++] = instance;
                return instance;
            }
            var newObjects = new T[2 * count];
            for (int n = 0; n < count; n++) {
                newObjects[n] = objects[n];                
            }
            objects             = newObjects;
            objects[count++]    = instance;
            return instance;
        }
        
        internal string GetString() {
            if (objects == null)
                return "";
            var type        = objects[0].GetType();
            var typeName    = VarType.GetTypeName(type);
            return $"count: {count}, used: {used} - {typeName}";
        }
    }
}