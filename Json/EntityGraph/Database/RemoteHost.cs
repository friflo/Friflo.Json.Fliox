// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Mapper;


namespace Friflo.Json.EntityGraph.Database
{
    public class RemoteHost : EntityDatabase
    {
        private readonly EntityDatabase local;
        private readonly JsonMapper     jsonMapper;
        
        public RemoteHost(EntityDatabase local) {
            jsonMapper = new JsonMapper();
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, database);
            HostContainer container = new HostContainer(name, this, localContainer);
            return container;
        }

        public override SyncResponse Execute(SyncRequest syncRequest) {
            var jsonRequest = jsonMapper.Write(syncRequest);
            var response = new SyncResponse();
            return response;
        }
    }
    
    public class HostContainer : EntityContainer
    {
        private readonly EntityContainer local;

        public HostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override void CreateEntities(ICollection<KeyValue> entities) {
            local.CreateEntities(entities);
        }

        public override void UpdateEntities(ICollection<KeyValue> entities) {
            local.UpdateEntities(entities);
        }

        public override ICollection<KeyValue> ReadEntities(ICollection<string> ids) {
            var result = local.ReadEntities(ids);
            return result;
        }
    }
}
