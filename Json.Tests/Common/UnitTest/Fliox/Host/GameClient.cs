using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public class GameClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <int, Player>     players;

        public GameClient(FlioxHub hub, string dbName = null)
            : base (hub, dbName, new ClientOptions (CreateEventReceiver))
        { }
        
        private static EventReceiver CreateEventReceiver(FlioxHub hub, FlioxClient client) {
            return new TestEventReceiver();
        }
    }
        
    public class Player
    {
        public int id;
    }
    
    /// <summary> Used to test performance and memory usage of <see cref="EventDispatcher"/>.EnqueueSyncTasks() </summary>
    public class TestEventReceiver : EventReceiver
    {
        protected override string  Endpoint           => nameof(TestEventReceiver);
        protected override bool    IsOpen()           => true;
        protected override bool    IsRemoteTarget()   => true;
        protected override void    SendEvent(in ClientEvent clientEvent) { }
    }
}