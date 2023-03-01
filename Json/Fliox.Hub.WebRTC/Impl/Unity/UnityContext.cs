// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Collections;
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
        internal static readonly UnityWebRtc Context = new UnityWebRtc();
            
        internal Task Await(AsyncOperationBase asyncOperation) {
            return Task.CompletedTask;
        }
    }
}

#endif