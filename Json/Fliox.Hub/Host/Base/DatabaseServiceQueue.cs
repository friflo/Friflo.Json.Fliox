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
        
        /// <summary>Ensure subsequent request executions run on the same thread</summary>
        private     const       bool                RunOnCallingThread      =  true;


        public DatabaseServiceQueue () {
            serviceJobs = new Queue<ServiceJob>();
            jobBuffer   = new List<ServiceJob>();
        }
        
        /// <summary>
        /// Execute queued tasks in case request queueing is enabled in the <see cref="DatabaseService"/> constructor
        /// </summary>
        public async Task<int> ExecuteQueuedRequestsAsync()
        {
            FillRequestBuffer();
            foreach (var job in jobBuffer) {
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
            return jobBuffer.Count;
        }
        
        private void FillRequestBuffer() {
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
        internal readonly FlioxHub                                  hub;
        internal readonly SyncRequest                               syncRequest;
        internal readonly SyncContext                               syncContext;
        internal readonly TaskCompletionSource<ExecuteSyncResult>   taskCompletionSource;
            
        internal ServiceJob(FlioxHub hub, SyncRequest syncRequest, SyncContext syncContext) {
            this.hub                = hub;
            this.syncRequest        = syncRequest;
            this.syncContext        = syncContext;
            taskCompletionSource    = new TaskCompletionSource<ExecuteSyncResult>();
        }
    }
}