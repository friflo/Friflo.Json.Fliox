// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Database.Utils {
    
    // [c# - ReaderWriterLockSlim and async\await - Stack Overflow] https://stackoverflow.com/questions/19659387/readerwriterlockslim-and-async-await
    //
    //   xtadex:
    // "Some time ago I implemented for my project class AsyncReaderWriterLock based on two SemaphoreSlim. Hope it can help.
    // It is implemented the same logic (Multiple Readers and Single Writer) and at the same time support async/await pattern.
    // Definitely, it does not support recursion and has no protection from incorrect usage.
    //
    // "You right AcquireReaderLock acquire firstly _writeSemaphore to make sure no any writers in play at this moment.
    // Write semaphore released immediately once _readerLock acquired. If you noticed, Read/Write acquire methods wait for both semaphores.
    // So SafeAcquireReadSemaphore() used for cases when a cancellation happened in second WaitAsync() to properly release resources in case of OCE."
    //
    public sealed class AsyncReaderWriterLock : IDisposable
    {
        private  readonly   SemaphoreSlim   readSemaphore  = new SemaphoreSlim(1, 1);
        private  readonly   SemaphoreSlim   writeSemaphore = new SemaphoreSlim(1, 1);
        private             int             readerCount;

        public async Task AcquireWriterLock(CancellationToken token = default)
        {
            await writeSemaphore.WaitAsync(token).ConfigureAwait(false);
            await SafeAcquireReadSemaphore(token).ConfigureAwait(false);
        }

        public void ReleaseWriterLock()
        {
            readSemaphore.Release();
            writeSemaphore.Release();
        }

        public async Task AcquireReaderLock(CancellationToken token = default)
        {
            await writeSemaphore.WaitAsync(token).ConfigureAwait(false);

            if (Interlocked.Increment(ref readerCount) == 1) {
                await SafeAcquireReadSemaphore(token).ConfigureAwait(false);
            }
            writeSemaphore.Release();
        }

        public void ReleaseReaderLock()
        {
            if (Interlocked.Decrement(ref readerCount) == 0) {
                readSemaphore.Release();
            }
        }

        private async Task SafeAcquireReadSemaphore(CancellationToken token)
        {
            try {
                await readSemaphore.WaitAsync(token).ConfigureAwait(false);
            }
            catch {
                writeSemaphore.Release();

                throw;
            }
        }

        public void Dispose()
        {
            writeSemaphore.Dispose();
            readSemaphore.Dispose();
        }
    }
}