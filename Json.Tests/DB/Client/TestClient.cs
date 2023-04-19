using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Tests.DB.Client
{
    public class TestClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, TestOps>         testOps;
        public readonly EntitySet <string, TestEnumEntity>  testEnum;
        
        public TestClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
}