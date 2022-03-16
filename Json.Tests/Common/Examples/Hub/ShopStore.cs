using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;

// ReSharper disable All
namespace Friflo.Json.Tests.Common.Examples.Hub
{
    public class ShopStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <long, Article>     articles;
        
        public ShopStore(FlioxHub hub) : base(hub) { }
    }
    
    public class Article
    {
        public  long        id { get; set; }
        public  string      name;
    }
    
    public static class TestShopStore
    {
        [Test]
        public static async Task AccessDatabase() {
            var database    = new MemoryDatabase(); // or other database like: file-system, SQLite, Postgres, ...
            var hub         = new FlioxHub(database);
            var store       = new ShopStore(hub);
            
            store.articles.Create(new Article() { id = 1, name = "Bread" });
            
            await store.SyncTasks();
        }
    }
}