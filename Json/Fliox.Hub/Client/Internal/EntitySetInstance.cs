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
    internal sealed partial class InternSet<TKey, T> : EntitySetBase<T>  where T : class
    {
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following properties while debugging:
        //          Peers, Tasks
                        internal            SetIntern<TKey, T>          intern;         // Use intern struct as first field 
                        
        /// <summary> key: <see cref="Peer{T}.entity"/>.id </summary>
        [Browse(Never)] private  readonly   Dictionary<TKey, Peer<T>>   peerMap;        //  Note: must be private by all means
        
        /// <summary> enable access to entities in debugger. Not used internally. </summary>
        // Note: using Dictionary.Values is okay. The ValueCollection is instantiated only once for a Dictionary instance
        // ReSharper disable once UnusedMember.Local
                        private            IReadOnlyCollection<Peer<T>> Peers           => peerMap.Values;
        
        [Browse(Never)] internal            LocalEntities<TKey,T>       Local           => local   ??= new LocalEntities<TKey, T>(this);
        [Browse(Never)] private             LocalEntities<TKey,T>       local;
        /// <summary> Note! Must be called only from <see cref="LocalEntities{TKey,T}"/> to preserve maintainability </summary>
                        internal            Dictionary<TKey, Peer<T>>   GetPeers()      => peerMap;
                        public   override   string                      ToString()      => SetInfo.ToString();

        [Browse(Never)] internal override   SetInfo                     SetInfo         => GetSetInfo();
        [Browse(Never)] internal override   Type                        KeyType         => typeof(TKey);
        [Browse(Never)] internal override   Type                        EntityType      => typeof(T);
        
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        [Browse(Never)] internal override   bool                        WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>
        [Browse(Never)] internal override   bool                        WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }
        
        internal    InstanceBuffer<DeleteTask<TKey,T>>                  deleteBuffer;
        internal    InstanceBuffer<ReadTask<TKey, T>>                   readBuffer;

        /*
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        // private static class Static  { } */
        private static  readonly       EntityKeyT<TKey, T>         EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static  readonly       KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
        

        internal InternSet(string name, int index, FlioxClient client) : base (name, index, client) {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
            // intern    = new SetIntern<TKey, T>(this);
            peerMap = CreateDictionary<TKey,Peer<T>>();
        }
    }
}