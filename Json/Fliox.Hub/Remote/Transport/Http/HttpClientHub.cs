// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely using a <see cref="HttpClient"/>
    /// </summary>
    public sealed class HttpClientHub : SocketClientHub
    {
        private  readonly   string      endpoint;
        private  readonly   HttpClient  httpClient;
        
        public   override   string      ToString()  => $"{database.nameShort} - endpoint: {endpoint}";

        public HttpClientHub(string dbName, string endpoint, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(new RemoteDatabase(dbName), env, access)
        {
            this.endpoint   = endpoint;
            httpClient      = new HttpClient();
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }
        
        public override bool SupportPushEvents => false;
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                var mapper              = pooledMapper.instance;
                var writer              = mapper.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var jsonRequest         = RemoteMessageUtils.CreateProtocolMessage(syncRequest, writer);
                var content             = jsonRequest.AsByteArrayContent();
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
                
                try {
                    var httpResponse    = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);
                    
                    var bodyArray       = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    var jsonBody        = new JsonValue(bodyArray);
                    var response        = RemoteMessageUtils.ReadProtocolMessage (jsonBody, mapper.reader, out string error);
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