// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Remote
{
    public class HttpClientDatabase : RemoteClientDatabase
    {
        private readonly string         endpoint;
        private readonly HttpClient     httpClient;

        public HttpClientDatabase(string endpoint) {
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }

        protected override async Task<string> ExecuteSyncJson(string jsonSyncRequest) {
            var body = new StringContent(jsonSyncRequest);
            body.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, body).ConfigureAwait(false);
            return await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}