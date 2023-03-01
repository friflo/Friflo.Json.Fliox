// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;

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
        
        internal static readonly UnityWebRtc            Singleton   = new UnityWebRtc();
            
        internal async Task Await(AsyncOperationBase operation) {
            var asyncOperation = new AsyncOperation(operation);
            operations.Add(asyncOperation);
            await asyncOperation.tcs.Task.ConfigureAwait(false);
        }
        
        internal void ProcessOperations() {
            done.Clear();
            foreach (var operation in operations) {
                if (!operation.value.IsDone) {
                    continue;
                }
                done.Add(operation);
                operation.tcs.SetResult(true);
            }
            foreach (var op in done) {
                operations.Remove(op);
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