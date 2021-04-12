// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.EntityGraph.Database
{
    public class RemoteClient : EntityDatabase
    {
        private readonly JsonMapper     jsonMapper;
        private readonly string         endpoint;
        private readonly HttpClient     httpClient;
        
        public RemoteClient(string endpoint) {
            this.endpoint = endpoint;
            jsonMapper = new JsonMapper { Pretty = true };
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
            jsonMapper.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            ClientContainer container = new ClientContainer(name, this);
            return container;
        }

        public override SyncResponse Execute(SyncRequest syncRequest) {
            var jsonRequest = jsonMapper.Write(syncRequest);
            var body = new StringContent(jsonRequest);
            body.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            HttpResponseMessage httpResponse = httpClient.PostAsync(endpoint, body).Result;
            string jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;
            var response = jsonMapper.Read<SyncResponse>(jsonResponse);
            return response;
        }
    }
    
    public class ClientContainer : EntityContainer
    {
        public ClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override void CreateEntities(Dictionary<string, EntityValue> entities) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override void UpdateEntities(Dictionary<string, EntityValue> entities) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override Dictionary<string, EntityValue> ReadEntities(ICollection<string> ids) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
    }
}
