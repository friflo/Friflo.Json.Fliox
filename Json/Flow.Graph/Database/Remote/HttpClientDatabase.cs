// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

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

        protected override async Task<SyncJsonResult> ExecuteSyncJson(string jsonSyncRequest, SyncContext syncContext) {
            var content = new StringContent(jsonSyncRequest);
            content.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var body = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                SyncStatusType statusType;
                switch (httpResponse.StatusCode) {
                    case HttpStatusCode.OK:                     statusType = SyncStatusType.Ok;         break; 
                    case HttpStatusCode.BadRequest:             statusType = SyncStatusType.Error;      break;
                    case HttpStatusCode.InternalServerError:    statusType = SyncStatusType.Exception;  break;
                    default:                                    statusType = SyncStatusType.Exception;  break;
                }
                return new SyncJsonResult(body, statusType);
            }
            catch (HttpRequestException e) {
                var error = SyncError.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return SyncJsonResult.CreateSyncError(syncContext, error.ToString());
            }
        }
    }
}