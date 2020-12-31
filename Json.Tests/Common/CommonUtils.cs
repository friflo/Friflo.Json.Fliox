// Copyright (c) Ullrich Praetz. All rights reserved.  
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common
{
    public class CommonUtils
    {
        public static string GetBasePath() {
#if UNITY_5_3_OR_NEWER
	        string baseDir = UnityUtils.GetProjectFolder();
#else
            string baseDir = Directory.GetCurrentDirectory() + "/../../../";
#endif
            return baseDir;
        }
        
        public static Bytes fromFile (String path) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] data = File.ReadAllBytes(baseDir + path);
            ByteArray bytes = Arrays.CopyFrom(data);
            return new Bytes(bytes);
        }
    }

    public enum MemoryLog {
        Enabled,
        Disabled
    }
    

    public struct MemoryLogger
    {
#if JSON_BURST
        // Burst code does not support usage of managed objects. So no logging performed in this case
        public MemoryLogger(int size, int stepSize, MemoryLog memoryLog) { }
        public void Snapshot() { }
        public void AssertNoAllocations() { }
#else
        private long[] totalMemory;
        private int totalMemoryCount;
        private int snapshotCount;
        private int snapshotInterval;
        private MemoryLog memoryLog;
        
        public MemoryLogger(int size, int snapshotInterval, MemoryLog memoryLog) {
            this.memoryLog = memoryLog;
            totalMemory = new long[size];
            totalMemoryCount = 0;
            snapshotCount = 1; // Dont log memory snapshots in the first interval to give chance filling the buffers.
            this.snapshotInterval = snapshotInterval;
            GC.Collect();
        }

        public void Snapshot() {
            if (snapshotCount++ % snapshotInterval == 0)
                totalMemory[totalMemoryCount++] = GC.GetTotalMemory(false);
        }

        public void AssertNoAllocations() {
            if (memoryLog == MemoryLog.Disabled)
                return;
            if (totalMemoryCount < 2)
                Assert.Fail($"Gathered too few memory snapshots ({totalMemoryCount}). Decrease snapshotInterval ({snapshotInterval})");
                
            long initialMemory = totalMemory[0];
            for (int i = 1; i < totalMemoryCount; i++) {
                if (initialMemory == totalMemory[i])
                    continue;
                string msg = $"Unexpected memory allocations. Snapshot history (bytes):\n{MemorySnapshots()}";
                Assert.Fail(msg);
                // TestContext.Out.WriteLine(msg);
                return;
            }
        }

        public string MemorySnapshots() {
            var msg = new StringBuilder();
            for (int i = 0; i < totalMemoryCount; i++)
                msg.Append($"  {totalMemory[i]}\n");
            return msg.ToString();
        }
#endif
    }
}