// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.  
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Utils;
using NUnit.Framework;
#if !UNITY_5_3_OR_NEWER
    using FluentAssertions;
#endif

namespace Friflo.Json.Tests.Common.Utils
{
    public class TestBytes : IDisposable
    {
        public Bytes bytes = new Bytes(0);

        public void Dispose() {
            bytes.Dispose();
        }
    }

    public static class AssertUtils {
        public static void Equivalent<TExpectation>(TExpectation expect, TExpectation actual) {
#if !UNITY_5_3_OR_NEWER
            actual.Should().BeEquivalentTo(expect);
#endif
        }
        
        public static void AreSimilar(object expect, object actual) {
            var normalizedExpect    = expect.ToString();
            var actualExpect        = actual.ToString();
            if (normalizedExpect == null || actualExpect == null)
                throw new InvalidOperationException("AreSimilar() - ToString() of both parameter must not be null");
            
            normalizedExpect    = normalizedExpect.Replace(" ", string.Empty);
            actualExpect        = actualExpect.Replace(" ", string.Empty);
            if (normalizedExpect.Equals(actualExpect))
                return;
            Assert.Fail($"Expected: {expect}\nBut was:  {actual}");
        }
        
        public static void AreEqualTrimStack(string expect, string actual) {
            if (expect == null || actual == null)
                throw new InvalidOperationException("AreEqualTrimAt() - both parameter must not be null");
            var     lines = actual.Split('\n');
            var     sb = new StringBuilder();
            bool    firstLine = true;
            bool    foundMethodLine = false;
            foreach (var line in lines) {
                bool isMethodLine =
                    line.StartsWith("   at ") ||    // method formatted by CLR
                    line.StartsWith("  at ");       // method formatted by Unity       
                // ignore methods
                if (isMethodLine) {
                    foundMethodLine = true;
                    continue;
                }
                if (firstLine) {
                    firstLine = false;
                } else {
                    sb.Append('\n');
                }
                sb.Append(line);
            }
            actual = sb.ToString();
            if (!foundMethodLine) {
                Assert.Fail($"Expect methods in error ('   at ...')");    
            }
            if (expect.Equals(actual))
                return;
            Assert.Fail($"Expected: {expect}\nBut was:  {actual}");
        }
    }
    
    public static class CommonUtils
    {
        public static string GetBasePath(string folder = "") {
#if UNITY_5_3_OR_NEWER
            string baseDir = UnityUtils.GetProjectFolder();
#else
            // remove folder like ".bin/Debug/net6.0" which is added when running unit tests
            var projectFolder   = IsInUnitTest ?  "/../../../" : "/";
            string baseDir      = Directory.GetCurrentDirectory() + projectFolder;
#endif
            baseDir = Path.GetFullPath(baseDir + folder);
            return baseDir;
        }
        
        static CommonUtils()
        {
            var testAssemblyName    = "nunit.framework";
            var assemblies          = AppDomain.CurrentDomain.GetAssemblies();
            IsInUnitTest            = assemblies.Any(a => a.FullName.StartsWith(testAssemblyName));
        }

        private static bool IsInUnitTest { get; }
        
#if UNITY_EDITOR
        public static bool  IsUnityEditor () { return true; }
#else
        public static bool  IsUnityEditor () { return false; }
#endif
        
        public static Bytes FromFile (String path) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] data = File.ReadAllBytes(baseDir + path);
            Bytes dst = new Bytes(0);
            Arrays.ToBytes(ref dst, data);
            return dst;
        }
        
        public static void ToFile (String path, Bytes bytes) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] dst = new byte[bytes.Len];
            Arrays.ToManagedArray(dst, bytes);
            using (FileStream fileStream = new FileStream(baseDir + path, FileMode.Create)) {
                fileStream.Write(dst, 0, dst.Length);
            }
        }
    }
    
    public enum MemoryLog {
        Enabled,
        Disabled
    }
    

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public struct MemoryLogger
    {
#if UNITY_5_3_OR_NEWER
        // Unity does not support GC.GetAllocatedBytesForCurrentThread(). So no logging performed in this case
        public MemoryLogger(int size, int stepSize, MemoryLog memoryLog) { }
        public void Reset() { }
        public void Snapshot() { }
        public void AssertNoAllocations() { }
#else
        private readonly    long[]      totalMemory;
        private             int         totalMemoryCount;
        private             int         snapshotCount;
        private readonly    int         snapshotInterval;
        private readonly    MemoryLog   memoryLog;
        
        public MemoryLogger(int maxSnapshotCount, int snapshotInterval, MemoryLog memoryLog) {
            this.memoryLog          = memoryLog;
            totalMemory             = new long[maxSnapshotCount];
            totalMemoryCount        = 0;
            snapshotCount           = 1; // Don't log memory snapshots in the first interval to give chance filling the buffers.
            this.snapshotInterval   = snapshotInterval;
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
                totalMemory[totalMemoryCount++] = Mem.GetAllocatedBytes();
        }

        public void AssertNoAllocations() {
            if (memoryLog == MemoryLog.Disabled)
                return;
            if (totalMemoryCount < 2)
                NUnit.Framework.Assert.Fail($"Gathered too few memory snapshots ({totalMemoryCount}). Decrease snapshotInterval ({snapshotInterval})");
                
            long initialMemory = totalMemory[0];
            for (int i = 1; i < totalMemoryCount; i++) {
                if (initialMemory == totalMemory[i])
                    continue;
                string msg = $"Unexpected memory allocations. Snapshot history (bytes):\n{MemorySnapshots()}";
                NUnit.Framework.Assert.Fail(msg);
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