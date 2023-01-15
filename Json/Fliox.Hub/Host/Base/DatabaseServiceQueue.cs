// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public class DatabaseServiceQueue {
        // used non concurrent Queue<> to avoid heap allocation on Enqueue()
        private  readonly   Queue<RequestJob>   requestJobs;
        private  readonly   List<RequestJob>    requestBuffer;
        private             bool                executeQueueAsync;

        /// <summary>Ensure subsequent request executions run on the same thread</summary>
        private  const      bool                RunOnCallingThread      =  true;
        
        internal DatabaseServiceQueue () {
            requestJobs     = new Queue<RequestJob>();
            requestBuffer   = new List<RequestJob>();
        }
        
        public int ExecuteQueuedRequests(out bool executeAsync)
        {
            FillRequestBuffer();

            foreach (var job in requestBuffer) {
                if (job.syncRequest.intern.executionType != ExecutionType.Sync) {
                    executeQueueAsync = executeAsync = true;
                    return requestBuffer.Count;
                }
            }
            // case: all requests can be executed synchronous
            foreach (var job in requestBuffer) {
                var response = job.hub.ExecuteRequest (job.syncRequest, job.syncContext);
                job.taskCompletionSource.SetResult(response);
            }
            executeQueueAsync = executeAsync = false;
            return requestBuffer.Count;
        }
        
        /// <summary>
        /// Execute queued tasks in case request queueing is enabled in the <see cref="DatabaseService"/> constructor
        /// </summary>
        public async Task<int> ExecuteQueuedRequestsAsync()
        {
            if (executeQueueAsync) {
                executeQueueAsync = false;
            } else { 
                FillRequestBuffer();
            }
            foreach (var job in requestBuffer) {
                try {
                    var syncRequest = job.syncRequest;
                    ExecuteSyncResult response;
                    if (syncRequest.intern.executionType == ExecutionType.Sync) {
                        // ReSharper disable once MethodHasAsyncOverload
                        response =       job.hub.ExecuteRequest     (syncRequest, job.syncContext);
                    } else {
                        response = await job.hub.ExecuteRequestAsync(syncRequest, job.syncContext).ConfigureAwait(RunOnCallingThread);
                    }
                    job.taskCompletionSource.SetResult(response);
                } catch (Exception e) {
                    job.taskCompletionSource.SetException(e);
                }
            }
            return requestBuffer.Count;
        }
        
        private void FillRequestBuffer() {
            requestBuffer.Clear();
            lock (requestJobs) {
                foreach (var job in requestJobs) {
                    requestBuffer.Add(job);
                }
                requestJobs.Clear();
            }            
        }
        
        internal void EnqueueJob(in RequestJob requestJob) {
            lock (requestJobs) {
                requestJobs.Enqueue(requestJob);
            }
        }
    }
    
    internal readonly struct RequestJob
    {
        internal readonly FlioxHub                                  hub;
        internal readonly SyncRequest                               syncRequest;
        internal readonly SyncContext                               syncContext;
        internal readonly TaskCompletionSource<ExecuteSyncResult>   taskCompletionSource;
            
        internal RequestJob(FlioxHub hub, SyncRequest syncRequest, SyncContext syncContext) {
            this.hub                = hub;
            this.syncRequest        = syncRequest;
            this.syncContext        = syncContext;
            taskCompletionSource    = new TaskCompletionSource<ExecuteSyncResult>();
        }
    }
}