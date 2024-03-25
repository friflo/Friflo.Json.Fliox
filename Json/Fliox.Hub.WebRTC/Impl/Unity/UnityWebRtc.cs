// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    /// <summary>
    /// Used to transform Unity WebRTC methods returning <see cref="AsyncOperationBase"/> - basically an
    /// <see cref="IEnumerator"/> - to an asynchronous method
    /// </summary>
    public class UnityWebRtc
    {
        private         readonly List<AsyncOperation>   operations  = new List<AsyncOperation>();
        private         readonly List<AsyncOperation>   done        = new List<AsyncOperation>();
        
        public static   readonly UnityWebRtc            Singleton   = new UnityWebRtc();
            
        internal async Task Await(AsyncOperationBase operation) {
            var asyncOperation = new AsyncOperation(operation);
            lock (operations) {
                operations.Add(asyncOperation);
            }
            await asyncOperation.tcs.Task.ConfigureAwait(false);
        }
        
        public void ProcessOperations() {
            done.Clear();
            lock (operations) {
                foreach (var operation in operations) {
                    if (!operation.value.IsDone) {
                        continue;
                    }
                    done.Add(operation);
                }
                foreach (var op in done) {
                    operations.Remove(op);
                }
            }
            foreach (var op in done) {
                op.tcs.SetResult(true);
            }
        }
    }
    
    internal readonly struct AsyncOperation {
        internal readonly AsyncOperationBase            value;
        internal readonly TaskCompletionSource<bool>    tcs;
        
        internal AsyncOperation(AsyncOperationBase value) {
            this.value  = value;
            tcs         = new TaskCompletionSource<bool>(false);
        }
    }
}

#endif