// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;

namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    public class HttpClientDatabase : RemoteClientDatabase
    {
        private  readonly   string          endpoint;
        private  readonly   HttpClient      httpClient;

        public HttpClientDatabase(string endpoint) : base(ProtocolType.ReqResp){
            this.endpoint = endpoint;
            httpClient = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }

        protected override async Task<JsonResponse> ExecuteRequestJson(int requestId, string jsonSyncRequest, MessageContext messageContext) {
            var content = new StringContent(jsonSyncRequest);
            content.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var body = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                ResponseStatusType statusType;
                switch (httpResponse.StatusCode) {
                    case HttpStatusCode.OK:                     statusType = ResponseStatusType.Ok;         break; 
                    case HttpStatusCode.BadRequest:             statusType = ResponseStatusType.Error;      break;
                    case HttpStatusCode.InternalServerError:    statusType = ResponseStatusType.Exception;  break;
                    default:                                    statusType = ResponseStatusType.Exception;  break;
                }
                return new JsonResponse(body, statusType);
            }
            catch (HttpRequestException e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(messageContext, error.ToString(), ResponseStatusType.Exception);
            }
        }
    }
}