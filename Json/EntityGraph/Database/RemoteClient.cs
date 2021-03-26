// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Mapper;

namespace Friflo.Json.EntityGraph.Database
{
    public class RemoteClient : EntityDatabase
    {
        private readonly JsonMapper     jsonMapper;
        
        public RemoteClient(EntityDatabase local) {
            jsonMapper = new JsonMapper();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            ClientContainer container = new ClientContainer(name, this);
            return container;
        }

        public override SyncResponse Execute(SyncRequest syncRequest) {
            var jsonRequest = jsonMapper.Write(syncRequest);

            var response = new SyncResponse();
            return response;
        }
    }
    
    public class ClientContainer : EntityContainer
    {

        public ClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override void CreateEntities(ICollection<KeyValue> entities) {

        }

        public override void UpdateEntities(ICollection<KeyValue> entities) {

        }

        public override ICollection<KeyValue> ReadEntities(ICollection<string> ids) {
            return null;
        }
    }
}
