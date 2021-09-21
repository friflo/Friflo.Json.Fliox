// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    public class HttpClientDatabase : RemoteClientDatabase
    {
        private  readonly   string          endpoint;
        private  readonly   HttpClient      httpClient;

        public HttpClientDatabase(string endpoint) : base(){
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var jsonRequest = CreateSyncRequest(syncRequest, messageContext.pools);
            var content = jsonRequest.AsByteArrayContent();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
            
            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var bodyArray   = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                
                var jsonBody = new JsonUtf8(bodyArray);
                ProtocolMessage message = CreateProtocolMessage (jsonBody, messageContext.pools);
                switch (httpResponse.StatusCode) {
                    case HttpStatusCode.OK:
                        if (message is SyncResponse syncResponse)
                            return syncResponse;
                        if (message is ErrorResponse errorResp)
                            return new SyncResponse { error = errorResp };
                        break;
                    default:
                        if (message is ErrorResponse errResp)
                            return new SyncResponse { error = errResp };
                        break;
                }
                var errorResponse = new SyncResponse {
                    error = new ErrorResponse {
                        message = $"Request failed. http status code: {httpResponse.StatusCode}"
                    }
                };
                return errorResponse; 
            }
            catch (HttpRequestException e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                var errorResponse = new SyncResponse() {
                    error = new ErrorResponse() {
                        message = $"Request failed: Exception: {e.Message}"
                    }
                };
                return errorResponse;
            }
        }
    }
}