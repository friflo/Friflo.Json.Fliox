#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Language;
using NUnit.Framework;

// ReSharper disable All
namespace Friflo.Json.Tests.Common.Examples.Hub
{
    public class ShopClient : FlioxClient
    {
        // --- containers
        public readonly EntitySet <long, Article>     articles;
        
        // --- commands
        /// <summary>return 'hello ...!' with the given <paramref name="param"/></summary>
        public CommandTask<string>  Hello (string param)    => send.Command<string, string> (param);
        
        public ShopClient(FlioxHub hub) : base(hub) { }
    }
    
    public class Article
    {
        public  long        id { get; set; }
        public  string      name;
    }
    
    public class ShopCommands : IServiceCommands
    {
        [CommandHandler]
        private static Result<string> Hello(Param<string> param, MessageContext context) {
            if (!param.GetValidate(out string value, out string error)) {
                return Result.ValidationError(error);
            }
            return $"hello {value}!";
        } 
    }
    
    // -------------------------------- run example as unit tests --------------------------------
    public static class TestShopStore
    {
        /// <summary>
        /// Execute service commands: Hello and std.Stats <br/>
        /// Execute database command: Upsert
        /// </summary>
        [Test]
        public static async Task AccessDatabase() {
            var database    = new FileDatabase("shop_db", "./shop_db").AddCommands(new ShopCommands());
            // or other database implementations like: MemoryDatabase, SQLite, Postgres, ...
            var hub         = new FlioxHub(database);
            var client      = new ShopClient(hub);
            
            var hello           = client.Hello("World");
            var createArticle   = client.articles.Upsert(new Article() { id = 1, name = "Bread" });
            var stats           = client.std.Stats(null);

            await client.SyncTasks();
            
            Console.WriteLine(hello.Result);
            // output:  hello World!
            Console.WriteLine($"createArticle.Success: {createArticle.Success}");
            // output:  createArticle.Success: True
            foreach (var container in stats.Result.containers) {
                Console.WriteLine($"{container.name}: {container.count}");
            }
            // output:  articles: 1
        }
    }
    
    public static class TestSchemaGeneration
    {
        /// <summary>
        /// Generate schema model files (HTML, JSON Schema / OpenAPI, Typescript, C#, Kotlin) for <see cref="ShopClient"/>
        /// in the working directory.
        /// </summary>
        [Test]
        public static void GenerateSchemaModels() {
            var schemaModels = SchemaModel.GenerateSchemaModels(typeof(ShopClient));
            foreach (var schemaModel in schemaModels) {
                var folder = $"./schema/{schemaModel.type}";
                schemaModel.WriteFiles(folder);
            }
        }
    }
}
#endif