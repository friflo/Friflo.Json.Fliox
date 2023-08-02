using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Event
{
    public static class TestClientOptions
    {
        [Test]
        public static void TestClientOptions_EventReceiver_1() {
            var hub     = new FlioxHub(new MemoryDatabase("test"));
            var client  = new PocStore(hub);
            client.Options.EventReceiver = null;
            client.ClientId = "ddd";
            
            var e = Throws<InvalidOperationException> (() => {
                client.Options.EventReceiver = null;
            });
            AreEqual("cannot change EventReceiver after assigning ClientId", e.Message);
        }
        
        [Test]
        public static async Task TestClientOptions_EventReceiver_2() {
            var hub     = new FlioxHub(new MemoryDatabase("test"));
            var client  = new PocStore(hub);
            await client.SyncTasks();
            
            var e = Throws<InvalidOperationException> (() => {
                client.Options.EventReceiver = null;
            });
            AreEqual("cannot change EventReceiver after calling SyncTasks()", e.Message);

        }
    }
}