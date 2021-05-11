// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Net.Http;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database
{
    public class HttpRemoteClient : RemoteClientDatabase
    {
        private readonly string         endpoint;
        private readonly HttpClient     httpClient;

        public HttpRemoteClient(string endpoint) {
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }

        protected override async Task<string> ExecuteJson(string jsonSynRequest) {
            var body = new StringContent(jsonSynRequest);
            body.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, body);
            return await httpResponse.Content.ReadAsStringAsync();;
        }
    }
}