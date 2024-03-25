// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Pools;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

namespace Friflo.Json.Fliox.Hub.Remote.Tools
{
    public readonly struct RemoteRequest
    {
        /// <summary>
        /// After a client send a remote request to the host the <see cref="response"/> is used to await its completion
        /// </summary>
        public   readonly   TaskCompletionSource<ProtocolResponse>  response;
        internal readonly   ReaderPool                              responseReaderPool;   
        
        public RemoteRequest(SyncContext syncContext, CancellationTokenSource cancellationToken) {
            response            = new TaskCompletionSource<ProtocolResponse>();
            responseReaderPool  = syncContext.responseReaderPool; 
            
            syncContext.canceler = () => {
                cancellationToken.Cancel();
            };
        }
    }
    
    public sealed class RemoteRequestMap {
        private  readonly  Dictionary<int, RemoteRequest> requestMap = new Dictionary<int, RemoteRequest>();
        
        public void Add(int reqId, RemoteRequest request) {
            lock (requestMap) {
                requestMap.Add(reqId, request);    
            }
        }

        internal bool Remove (int reqId, out RemoteRequest request) {
            lock (requestMap) {
                return requestMap.Remove(reqId, out request);
            }
        }

        public void CancelRequests() {
            Dictionary<int, RemoteRequest>.ValueCollection requests;
            lock (requestMap) {
                requests = requestMap.Values;
            }
            foreach (var request in requests) {
                request.response.SetCanceled();
            }
        }
    }
}