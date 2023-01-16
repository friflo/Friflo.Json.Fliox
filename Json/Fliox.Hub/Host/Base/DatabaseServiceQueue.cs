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
        private     readonly    Queue<ServiceJob>   serviceJobs;
        private     readonly    List<ServiceJob>    jobBuffer;
        
        public DatabaseServiceQueue () {
            serviceJobs = new Queue<ServiceJob>();
            jobBuffer   = new List<ServiceJob>();
        }
        
        /// <summary>
        /// Execute queued tasks in case request queueing is enabled in the <see cref="DatabaseService"/> constructor
        /// </summary>
        public async Task<int> ExecuteQueuedRequestsAsync()
        {
            FillJobBuffers();
            foreach (var job in jobBuffer) {
                try {
                    var syncRequest = job.syncRequest;
                    ExecuteSyncResult response;
                    if (syncRequest.intern.executionType == ExecutionType.Sync) {
                        // ReSharper disable once MethodHasAsyncOverload
                        response =       job.hub.ExecuteRequest     (syncRequest, job.syncContext);
                    } else {
                        response = await job.hub.ExecuteRequestAsync(syncRequest, job.syncContext).ConfigureAwait(false);
                    }
                    job.taskCompletionSource.SetResult(response);
                } catch (Exception e) {
                    job.taskCompletionSource.SetException(e);
                }
            }
            return jobBuffer.Count;
        }
        
        /// <summary>
        /// Execute queued tasks in case request queueing is enabled in the <see cref="DatabaseService"/> constructor
        /// </summary>
        public int ExecuteQueuedRequests(out AsyncServiceJobs asyncServiceJobs)
        {
            FillJobBuffers();
            List<ServiceJob> asyncJobs = null;
            // Execute all synchronous requests to ensure execution stay on the calling thread
            foreach (var job in jobBuffer) {
                try {
                    var syncRequest = job.syncRequest;
                    if (syncRequest.intern.executionType == ExecutionType.Sync) {
                        var response =   job.hub.ExecuteRequest     (syncRequest, job.syncContext);
                        job.taskCompletionSource.SetResult(response);
                        continue;
                    }
                    if (asyncJobs == null) asyncJobs = new List<ServiceJob>(jobBuffer.Count);
                    asyncJobs.Add(job);
                    
                } catch (Exception e) {
                    job.taskCompletionSource.SetException(e);
                }
            }
            asyncServiceJobs = new AsyncServiceJobs(asyncJobs);
            return jobBuffer.Count;
        }
            
        /// <summary>Execute all async requests returned from <see cref="ExecuteQueuedRequests"/></summary>
        public static async Task ExecuteQueuedRequestsAsync(AsyncServiceJobs asyncServiceJobs) {
            var asyncJobs = asyncServiceJobs.asyncJobs;
            foreach (var job in asyncJobs) {
                try {
                    var syncRequest = job.syncRequest;
                    var response = await job.hub.ExecuteRequestAsync(syncRequest, job.syncContext).ConfigureAwait(false);
                    job.taskCompletionSource.SetResult(response);
                } catch (Exception e) {
                    job.taskCompletionSource.SetException(e);
                }
            }
        }
        
        private void FillJobBuffers() {
            jobBuffer.Clear();
            lock (serviceJobs) {
                foreach (var job in serviceJobs) {
                    jobBuffer.Add(job);
                }
                serviceJobs.Clear();
            }            
        }
        
        internal void EnqueueJob(in ServiceJob serviceJob) {
            lock (serviceJobs) {
                serviceJobs.Enqueue(serviceJob);
            }
        }
    }
    
    internal readonly struct ServiceJob
    {
        internal  readonly  FlioxHub                                hub;
        internal  readonly  SyncRequest                             syncRequest;
        internal  readonly  SyncContext                             syncContext;
        internal  readonly  TaskCompletionSource<ExecuteSyncResult> taskCompletionSource;
            
        internal ServiceJob(FlioxHub hub, SyncRequest syncRequest, SyncContext syncContext) {
            this.hub                = hub;
            this.syncRequest        = syncRequest;
            this.syncContext        = syncContext;
            taskCompletionSource    = new TaskCompletionSource<ExecuteSyncResult>();
        }
    }
    
    public readonly struct AsyncServiceJobs
    {
        internal  readonly  List<ServiceJob>    asyncJobs;
        public              int                 Count => asyncJobs?.Count ?? 0 ;
        
        internal AsyncServiceJobs (List<ServiceJob> asyncJobs) {
            this.asyncJobs = asyncJobs;
        }
    }
}