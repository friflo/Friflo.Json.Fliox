// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Utils;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed partial class EntitySetInstance<TKey, T> : EntitySetBase<T>  where T : class
    {
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following properties while debugging:
        //          Peers, Tasks
                        internal            SetIntern<TKey, T>          intern;         // Use intern struct as first field 
                        
        [Browse(Never)] internal readonly   FlioxClient                 client;
        /// <summary> available in debugger via <see cref="SetIntern{TKey,T}.SyncSet"/> </summary>
        [Browse(Never)] internal            SyncSet<TKey, T>            syncSet;
        /// <summary> key: <see cref="Peer{T}.entity"/>.id </summary>
        [Browse(Never)] private             Dictionary<TKey, Peer<T>>   peerMap;        //  Note: must be private by all means
        
        /// <summary> enable access to entities in debugger. Not used internally. </summary>
        // Note: using Dictionary.Values is okay. The ValueCollection is instantiated only once for a Dictionary instance
        // ReSharper disable once UnusedMember.Local
                        private             IReadOnlyCollection<Peer<T>> Peers => peerMap?.Values;
        
        [Browse(Never)] internal            LocalEntities<TKey,T>       Local           => local   ??= new LocalEntities<TKey, T>(this);
        [Browse(Never)] private             LocalEntities<TKey,T>       local;
        /// Note: must be private by all means
                        internal            Dictionary<TKey, Peer<T>>   PeerMap()       => peerMap ??= SyncSet.CreateDictionary<TKey,Peer<T>>();
        /// <summary> Note! Must be called only from <see cref="LocalEntities{TKey,T}"/> to preserve maintainability </summary>
                        internal            Dictionary<TKey, Peer<T>>   GetPeers()      => peerMap;
                        internal            SyncSet<TKey, T>            GetSyncSet()    => syncSet ??= syncSetBuffer.Get() ?? new SyncSet<TKey, T>(this);
                        internal override   SyncSetBase<T>              GetSyncSetBase()=> syncSet;
                        public   override   string                      ToString()      => SetInfo.ToString();

        [Browse(Never)] internal override   SyncSet                     SyncSet         => syncSet;
        [Browse(Never)] internal override   SetInfo                     SetInfo         => GetSetInfo();
        [Browse(Never)] internal override   Type                        KeyType         => typeof(TKey);
        [Browse(Never)] internal override   Type                        EntityType      => typeof(T);
        
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        [Browse(Never)] internal override   bool                        WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>
        [Browse(Never)] internal override   bool                        WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }
        
        internal    InstanceBuffer<DeleteTask<TKey,T>>                  deleteBuffer;
        internal    InstanceBuffer<ReadTask<TKey, T>>                   readBuffer;
        internal    InstanceBuffer<SyncSet<TKey,T>>                     syncSetBuffer;

        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        private static class Static  {
            internal static  readonly       EntityKeyT<TKey, T>         EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
            internal static  readonly       KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
        }

        internal EntitySetInstance(string name, FlioxClient client) : base (name) {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
            // intern    = new SetIntern<TKey, T>(this);
            this.client         = client;
            intern.entitySet    = this;
        }
    }
}