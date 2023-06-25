using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable All
namespace Lab
{
    public class LabClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <long, Article>     articles;

        public LabClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
    }
}
