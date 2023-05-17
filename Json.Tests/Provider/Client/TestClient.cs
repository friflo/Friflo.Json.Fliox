using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Tests.Provider.Client
{
    public class TestClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, TestMutate>      testMutate;
        public readonly EntitySet <string, TestOps>         testOps;
        public readonly EntitySet <string, TestQuantify>    testQuantify;
        public readonly EntitySet <string, CompareScalar>   compare;
        public readonly EntitySet <string, TestString>      testString;
        public readonly EntitySet <string, TestEnumEntity>  testEnum;
        public readonly EntitySet <string, CursorEntity>    testCursor;

        public TestClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
}