// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestWriteRead      () { await TestCreate(async (store) => await AssertWriteRead        (store)); }
        
        private static async Task AssertWriteRead(PocStore createStore) {
            // --- cache empty
            var readOrders  = createStore.orders.Read();
            var orderTask   = readOrders.Find("order-1");
            await createStore.Sync();

            var order = orderTask.Result;
            using (ObjectMapper mapper = new ObjectMapper(createStore.TypeStore)) {
                mapper.Pretty = true;
            
                AssertWriteRead(mapper, order);
                AssertWriteRead(mapper, order.customer);
                AssertWriteRead(mapper, order.items[0]);
                AssertWriteRead(mapper, order.items[1]);
                AssertWriteRead(mapper, order.items[0].article);
                AssertWriteRead(mapper, order.items[1].article);
            }
        }
        
        private static void AssertWriteRead<T>(ObjectMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}