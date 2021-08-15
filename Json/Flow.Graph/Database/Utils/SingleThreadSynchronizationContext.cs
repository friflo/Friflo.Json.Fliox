// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Utils
{
    // [Await, SynchronizationContext, and Console Apps | .NET Parallel Programming] https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private  readonly   BlockingCollection<ActionPair>  queue = new BlockingCollection<ActionPair>();

        public override void Post(SendOrPostCallback callback, object state) {
            var action = new ActionPair {callback = callback, state = state};
            queue.Add(action);
        }

        private void RunOnCurrentThread() {
            while (queue.TryTake(out ActionPair action, Timeout.Infinite))
                action.callback(action.state);
            Console.WriteLine("RunOnCurrentThread exited.");
        }

        private void Complete()
        {
            queue.CompleteAdding();
            Console.WriteLine("SingleThreadSynchronizationContext Completed.");
        }

        public static void Run(Func<Task> func)
        {
            var prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                Task funcTask = func();
                funcTask.ContinueWith(t => syncCtx.Complete(), TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();

                funcTask.GetAwaiter().GetResult();
            }
            finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
        }
    }
    
    internal struct ActionPair {
        internal SendOrPostCallback  callback;
        internal object              state;
    }
}