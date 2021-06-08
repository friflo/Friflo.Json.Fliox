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

        protected override async Task<JsonResponse> ExecuteRequestJson(string jsonSyncRequest, SyncContext syncContext) {
            var content = new StringContent(jsonSyncRequest);
            content.Headers.ContentType.MediaType = "application/json";
            // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};

            try {
                HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                var body = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                RequestStatusType statusType;
                switch (httpResponse.StatusCode) {
                    case HttpStatusCode.OK:                     statusType = RequestStatusType.Ok;         break; 
                    case HttpStatusCode.BadRequest:             statusType = RequestStatusType.Error;      break;
                    case HttpStatusCode.InternalServerError:    statusType = RequestStatusType.Exception;  break;
                    default:                                    statusType = RequestStatusType.Exception;  break;
                }
                return new JsonResponse(body, statusType);
            }
            catch (HttpRequestException e) {
                var error = ResponseError.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                return JsonResponse.CreateResponseError(syncContext, error.ToString(), RequestStatusType.Exception);
            }
        }
    }
}