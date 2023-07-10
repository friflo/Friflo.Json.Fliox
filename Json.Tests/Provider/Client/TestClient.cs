using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Provider.Client
{
    public class TestClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, TestMutate>          testMutate;
        public readonly EntitySet <string, TestOps>             testOps;
        public readonly EntitySet <string, TestQuantify>        testQuantify;
        public readonly EntitySet <string, CompareScalar>       compare;
        public readonly EntitySet <string, TestString>          testString;
        public readonly EntitySet <string, TestEnumEntity>      testEnum;
        public readonly EntitySet <string, CursorEntity>        testCursor;
        public readonly EntitySet <int,    TestIntKeyEntity>    testIntKey;
        public readonly EntitySet <Guid,   TestGuidKeyEntity>   testGuidKey;
        public readonly EntitySet <string, TestKeyName>         testKeyName;
        public readonly EntitySet <string, TestReadTypes>       testReadTypes;

        /// <summary>Drop the given database or all is param is null</summary>
        public CommandTask<List<String>>      DropDatabase (string param)    => send.Command<string, List<String>>    (param);

        public TestClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
}