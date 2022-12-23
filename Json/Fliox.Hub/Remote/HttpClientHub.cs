// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely using a <see cref="HttpClient"/>
    /// </summary>
    public sealed class HttpClientHub : RemoteClientHub
    {
        private  readonly   string      endpoint;
        private  readonly   HttpClient  httpClient;
        
        public   override   string      ToString()  => $"{database.name} - endpoint: {endpoint}";

        public HttpClientHub(string dbName, string endpoint, SharedEnv env = null)
            : base(new RemoteDatabase(dbName), env)
        {
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }
        
        public override bool SupportPushEvents => false;
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            base.InitSyncRequest(syncRequest);
            return ExecutionType.Async;
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                var mapper          = pooledMapper.instance;
                var jsonRequest     = RemoteUtils.CreateProtocolMessage(syncRequest, mapper.writer);
                var content         = jsonRequest.AsByteArrayContent();
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
                
                try {
                    var httpResponse    = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                    
                    var bodyArray       = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    var jsonBody        = new JsonValue(bodyArray);
                    var response        = RemoteUtils.ReadProtocolMessage (jsonBody, mapper, out string error);
                    switch (response) {
                        case null:
                            return  new ExecuteSyncResult(error, ErrorResponseType.BadResponse);
                        case SyncResponse syncResponse:
                            if (httpResponse.StatusCode == HttpStatusCode.OK) {
                                return new ExecuteSyncResult(syncResponse);
                            }
                            var msg = $"Request failed. StatusCode: {httpResponse.StatusCode}, error: {jsonBody.AsString()}";
                            return new ExecuteSyncResult(msg, ErrorResponseType.BadResponse);
                        case ErrorResponse errorResponse:
                            return new ExecuteSyncResult(errorResponse.message, errorResponse.type);
                        default:
                            var msg2 = $"Unknown response. StatusCode: {httpResponse.StatusCode}, Type: {response.GetType().Name}";
                            return new ExecuteSyncResult(msg2, ErrorResponseType.BadResponse);
                    }
                }
                catch (HttpRequestException e) {
                    var error = ErrorResponse.ErrorFromException(e);
                    error.Append(" endpoint: ");
                    error.Append(endpoint);
                    var msg = $"Request failed: Exception: {error}";
                    return new ExecuteSyncResult(msg, ErrorResponseType.Exception);
                }
            }
        }
    }
}