// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public static class TestJobQueuePoc
    {
        private static readonly ConcurrentQueue<Job> Queue = new ConcurrentQueue<Job>();

        [UnityTest] public static IEnumerator  TestConcurrentQueueAsync_Unity() { yield return RunAsync.Await(ConcurrentQueueAsync()); }
        [Test]      public static async Task   TestConcurrentQueueAsync() { await ConcurrentQueueAsync(); }

        private static async Task ConcurrentQueueAsync() {
            
            var thread = new Thread(() =>
            {
                Console.WriteLine($"queue - thread {Environment.CurrentManagedThreadId}");

                while(true) {
                    if (!Queue.TryDequeue(out var job))
                        continue;
                    if (job == null)
                        return;
                    job.action();
                    job.tcs.SetResult(11);
                }
            });
            thread.Start();
            
            var myJob = new Job(() => {
                Console.WriteLine($"myJob - thread {Environment.CurrentManagedThreadId}");
            });
            Queue.Enqueue(myJob);
            await myJob.tcs.Task.ConfigureAwait(false);
            Queue.Enqueue(null);
        }

        private sealed class Job
        {
            internal readonly TaskCompletionSource<int>  tcs = new TaskCompletionSource<int>();
            internal readonly Action                     action;
            
            internal Job(Action action) {
                this.action = action;
            }
        }
    }
}
