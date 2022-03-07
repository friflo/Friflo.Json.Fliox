using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Tests.Common.Examples.Hub
{
    public class MyStore : FlioxClient
    {
        public MyCommands test;
        
        public MyStore(FlioxHub hub) : base(hub) {
            test = new MyCommands(this);
        }
    }
    
    public class MyCommands : HubMessages
    {
        public MyCommands(FlioxClient client) : base(client) { }
        
        public CommandTask<string> Cmd (string param) => SendCommand<string, string>("test.Cmd", param);
    }
}