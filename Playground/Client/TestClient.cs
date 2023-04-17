using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Playground.Client
{
    public class TestClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, Article>         articles;
        public readonly EntitySet <string, Customer>        customers;
        public readonly EntitySet <string, Employee>        employees;
        public readonly EntitySet <string, Order>           orders;
        public readonly EntitySet <string, Producer>        producers;
        public readonly EntitySet <string, TestEnumEntity>  testEnum;
        
        public TestClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
}