// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
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
    public sealed class HttpClientHub : FlioxHub
    {
        private  readonly   string                  endpoint;
        private  readonly   HttpClient              httpClient;
        
        public   override   string      ToString()  => $"{database.nameShort} - endpoint: {endpoint}";

        public HttpClientHub(string dbName, string endpoint, SharedEnv env = null)
            : base(new RemoteDatabase(dbName), env)
        {
            this.endpoint       = endpoint;
            httpClient          = new HttpClient();
            // httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            // httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        }
        
        public override void Dispose() {
            base.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }
        
        protected internal  override    bool    SupportPushEvents   => false;
        protected internal  override    bool    IsRemoteHub         => true;
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            return ExecutionType.Async;
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            // requires its own mapper - method can be called from multiple threads simultaneously
            using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                var mapper              = pooledMapper.instance;
                var writer              = MessageUtils.GetPrettyWriter(mapper);
                var jsonRequest         = MessageUtils.WriteProtocolMessage(syncRequest, sharedEnv, writer);
                var content             = jsonRequest.AsByteArrayContent();
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // body.Headers.ContentEncoding = new string[]{"charset=utf-8"};
                
                try {
                    var httpResponse    = await httpClient.PostAsync(endpoint, content).ConfigureAwait(false);

                    var bodyArray       = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    var gzip            = httpResponse.Content.Headers.ContentEncoding.Contains("gzip"); 
                    var jsonBody        = gzip ? ReadGzipStream(bodyArray) : new JsonValue(bodyArray);
                    
                    var response        = MessageUtils.ReadProtocolMessage (jsonBody, sharedEnv, mapper.reader, out string error);
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
        
        private static JsonValue ReadGzipStream(byte[] bodyArray) {
            var stream          = new MemoryStream(bodyArray);
            var outStream       = new MemoryStream();
            var zipStream       = new GZipStream(stream, CompressionMode.Decompress, false);
#if NETSTANDARD2_0
            byte[] buffer       = new  byte[1024];
#else
            Span<byte> buffer   = stackalloc byte[1024];
#endif
            while (true) {
#if NETSTANDARD2_0
                int len     = zipStream.Read(buffer, 0, buffer.Length);
                outStream.Write(buffer, 0, len);
#else
                int len     = zipStream.Read(buffer);
                var span    = buffer.Slice(0, len);
                outStream.Write(span);
#endif
                if (len > 0) {
                    continue;
                }
                var result = new JsonValue(outStream.GetBuffer(), (int)outStream.Length); 
                return result;
            }
        }
    }
}