using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable All
namespace Friflo.Json.Tests.Common.Examples.Hub
{
    public class TestStore : FlioxClient
    {
        // --- commands
        public MyCommands test;
        
        public TestStore(FlioxHub hub) : base(hub) {
            test = new MyCommands(this);
        }
    }
    
    public class MyCommands : HubMessages
    {
        public MyCommands(FlioxClient client) : base(client) { }
        
        public CommandTask<string> Cmd (string param) => SendCommand<string, string>("test.Cmd", param);
    }
}