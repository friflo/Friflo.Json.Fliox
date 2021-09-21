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
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var jsonRequest = RemoteUtils.CreateProtocolMessage(syncRequest, messageContext.pools);
            var content = jsonRequest.AsByteArrayContent();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
            
            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var bodyArray   = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var jsonBody    = new JsonUtf8(bodyArray);
                var response    = RemoteUtils.ReadProtocolMessage (jsonBody, messageContext.pools, out string error);
                switch (response) {
                    case null:
                        return  new MsgResponse<SyncResponse>(error);
                    case SyncResponse syncResponse:
                        if (httpResponse.StatusCode == HttpStatusCode.OK) {
                            return new MsgResponse<SyncResponse>(syncResponse);
                        }
                        var msg = $"Request failed. StatusCode: {httpResponse.StatusCode}, error: {jsonBody.AsString()}";
                        return new MsgResponse<SyncResponse>(msg);
                    case ErrorResponse errorResponse:
                        return new MsgResponse<SyncResponse>(errorResponse.message);
                    default:
                        var msg2 = $"Unknown response. StatusCode: {httpResponse.StatusCode}, Type: {response.GetType().Name}";
                        return new MsgResponse<SyncResponse>(msg2);
                }
            }
            catch (HttpRequestException e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                var msg = $"Request failed: Exception: {error}";
                return new MsgResponse<SyncResponse>(msg);
            }
        }
    }
}