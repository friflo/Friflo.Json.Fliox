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
            : base (hub, dbName)
        {
            Options.DebugEventReceiver = new TestEventReceiver();
        }
    }
        
    public class Player
    {
        public int id;
    }
    
    /// <summary> Used to test performance and memory usage of <see cref="EventDispatcher"/>.EnqueueSyncTasks() </summary>
    public class TestEventReceiver : IEventReceiver
    {
        public  string  Endpoint           => nameof(TestEventReceiver);
        public  bool    IsOpen()           => true;
        public  bool    IsRemoteTarget()   => true;
        public  void    SendEvent(in ClientEvent clientEvent) { }
    }
}