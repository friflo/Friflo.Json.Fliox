using System;

namespace Friflo.Json.Burst.Utils
{
    
    public enum MemoryLog {
        Enabled,
        Disabled
    }
    
    public struct MemoryLogger
    {
#if JSON_BURST || UNITY_5_3_OR_NEWER
        // Burst code does not support usage of managed objects. So no logging performed in this case
        public MemoryLogger(int size, int stepSize, MemoryLog memoryLog, Action<string> assertFail) { }
        public void Reset() { }
        public void Snapshot() { }
        public void AssertNoAllocations() { }
#else
        private long[] totalMemory;
        private int totalMemoryCount;
        private int snapshotCount;
        private int snapshotInterval;
        private MemoryLog memoryLog;
        private Action<string> assertFail;
        
        public MemoryLogger(int size, int snapshotInterval, MemoryLog memoryLog, Action<string> assertFail) {
            this.memoryLog = memoryLog;
            this.assertFail = assertFail;
            totalMemory = new long[size];
            totalMemoryCount = 0;
            snapshotCount = 1; // Dont log memory snapshots in the first interval to give chance filling the buffers.
            this.snapshotInterval = snapshotInterval;
            GC.Collect();
        }

        public void Reset() {
            totalMemoryCount = 0;
            snapshotCount = 0;
        }

        public void Snapshot() {
            if (memoryLog == MemoryLog.Disabled)
                return;
            if (snapshotCount++ % snapshotInterval == 0)
                totalMemory[totalMemoryCount++] = GC.GetAllocatedBytesForCurrentThread();
        }

        public void AssertNoAllocations() {
            if (memoryLog == MemoryLog.Disabled)
                return;
            if (totalMemoryCount < 2)
                assertFail($"Gathered too few memory snapshots ({totalMemoryCount}). Decrease snapshotInterval ({snapshotInterval})");
                
            long initialMemory = totalMemory[0];
            for (int i = 1; i < totalMemoryCount; i++) {
                if (initialMemory == totalMemory[i])
                    continue;
                string msg = $"Unexpected memory allocations. Snapshot history (bytes):\n{MemorySnapshots()}";
                assertFail(msg);
                return;
            }
        }

        public string MemorySnapshots() {
            var msg = new System.Text.StringBuilder();
            for (int i = 0; i < totalMemoryCount; i++)
                msg.Append($"  {totalMemory[i]}\n");
            return msg.ToString();
        }
#endif
    }
}