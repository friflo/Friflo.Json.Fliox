// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Tests.Common.Utils;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestConcurrentAccess      () { await AssertConcurrentAccess(); }
        
        private static async Task AssertConcurrentAccess() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var read1        = new PocStore(fileDatabase, "read-1"))
            using (var read2        = new PocStore(fileDatabase, "read-2"))
            using (var write1       = new PocStore(fileDatabase, "write-1"))
            using (var write2       = new PocStore(fileDatabase, "write-2"))
            {
                var id = "concurrent-access";
                var employee = new Employee { id = id, firstName = "Concurrent accessed entity"};
                write1.employees.Create(employee);
                await write1.Sync();
                
                var read1Task   = ReadLoop (read1, id);
                var read2Task   = ReadLoop (read2, id);
                var write1Task  = WriteLoop(write1, employee);
                var write2Task  = WriteLoop(write2, employee);

                await Task.WhenAll(read1Task, read2Task, write1Task, write2Task);
            }
        }
        
        private static async Task ReadLoop (PocStore store, string id) {
            for (int n= 0; n < 20; n++) {
                var readEmployee = store.employees.Read();
                readEmployee.Find(id);
                await store.Sync();
            }
        }
        
        private static async Task WriteLoop (PocStore store, Employee employee) {
            for (int n= 0; n < 10; n++) {
                store.employees.Create(employee);
                await store.Sync();
            }
        }
    }
}