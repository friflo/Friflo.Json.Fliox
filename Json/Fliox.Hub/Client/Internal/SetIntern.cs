// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct SetIntern<TKey, T> where T : class
    {
        [DebuggerBrowsable(Never)]
        internal            TypeMapper<T>       typeMapper;     // set/create on demand
        private             List<TKey>          keysBuf;        // create on demand
        internal            SubscribeChanges    subscription;
        internal            bool                writePretty;
        internal            bool                writeNull;
        
        // private static readonly EntityKeyT<TKey, T> EntityKeyTMap       = EntityKey.GetEntityKeyT<TKey, T>();
        
        public    override  string              ToString()  => "";
        
        internal List<TKey>     GetKeysBuf()    => keysBuf      ??= new List<TKey>();

        /*
        internal SetIntern(EntitySet<TKey,T> entitySet) {
            typeMapper      = null;
            this.entitySet  = entitySet;
            subscription    = null;
            keysBuf         = null;
            writePretty     = ClientStatic.DefaultWritePretty;
            writeNull       = ClientStatic.DefaultWriteNull;
        } */
    }
}
