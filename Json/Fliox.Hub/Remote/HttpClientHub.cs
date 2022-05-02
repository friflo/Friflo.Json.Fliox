// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public class HttpClientHub : RemoteClientHub
    {
        private  readonly   string          endpoint;
        private  readonly   HttpClient      httpClient;

        public HttpClientHub(string endpoint, SharedEnv env = null) : base(null, env) {
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }
        
        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, ExecuteContext executeContext) {
            var jsonRequest = RemoteUtils.CreateProtocolMessage(syncRequest, executeContext.ObjectMapper);
            var content = jsonRequest.AsByteArrayContent();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
            
            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var bodyArray   = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var jsonBody    = new JsonValue(bodyArray);
                var response    = RemoteUtils.ReadProtocolMessage (jsonBody, executeContext.ObjectMapper, out string error);
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