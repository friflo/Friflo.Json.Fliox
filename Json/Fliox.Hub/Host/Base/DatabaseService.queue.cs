// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public partial class DatabaseService
    {
        private readonly struct Queue {
            // used non concurrent Queue<> to avoid heap allocation on Enqueue()
            internal readonly   Queue<RequestJob>   requestJobs;
            internal readonly   List<RequestJob>    requestBuffer;
            
            internal Queue (bool queueRequests) {
                requestJobs     = queueRequests ? new Queue<RequestJob>() : null;
                requestBuffer   = queueRequests ? new List<RequestJob>()  : null;
            }
            
            internal void FillRequestBuffer() {
                requestBuffer.Clear();
                lock (requestJobs) {
                    foreach (var job in requestJobs) {
                        requestBuffer.Add(job);
                    }
                    requestJobs.Clear();
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
        
        private const string QueueError = "queueing requests requires a DatabaseService with queueRequests enabled";
        
        internal void EnqueueJob(in RequestJob requestJob) {
            var requestJobs = queue.requestJobs;
            if (requestJobs == null) throw new InvalidOperationException(QueueError);
            lock (requestJobs) {
                requestJobs.Enqueue(requestJob);
            }
        }
        
        public int ExecuteQueuedRequests(out bool executeAsync) {
            queue.FillRequestBuffer();
            var requestBuffer = queue.requestBuffer;
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
        public async Task<int> ExecuteQueuedRequestsAsync() {
            if (executeQueueAsync) {
                executeQueueAsync = false;
            } else { 
                queue.FillRequestBuffer();
            }
            var requestBuffer = queue.requestBuffer;
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
    }
}