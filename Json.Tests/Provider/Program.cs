using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.MySQL;
using Friflo.Json.Fliox.Hub.PostgreSQL;
using Friflo.Json.Fliox.Hub.Redis;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.SQLite;
using Friflo.Json.Fliox.Hub.SQLServer;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Provider.Client;

namespace Friflo.Json.Tests.Provider
{
    public class Program
    {
      
        public static async Task Run()
        {
            var host            = await CreateHttpHost();
            var httpListener    = new HttpListener();
            httpListener.Prefixes.Add("http://+:8011/");
            var server          = new HttpServer(httpListener, host);
            server.Start();
            server.Run();
        }
        
        private static readonly DatabaseSchema Schema      = DatabaseSchema.Create<TestClient>();
        
        private static async Task<HttpHost> CreateHttpHost() {
            var env         = new SharedEnv();
            string cache    = null;

            var fileDb      = new FileDatabase("file_db", Env.TestDbFolder, Schema);
            var memoryDb    = new MemoryDatabase("memory_db", Schema);
            await memoryDb.SeedDatabase(fileDb).ConfigureAwait(false);
            memoryDb.AddCommands(new TestDBCommands());
            
            var hub         = new FlioxHub(memoryDb, env);
            hub.Info.Set("Test DB", "test", "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Provider", "rgb(0 140 255)");
            hub.AddExtensionDB (fileDb);
            
            AddDatabases(hub, Schema);
            await hub.SetupDatabasesAsync().ConfigureAwait(false);

            hub.AddExtensionDB  (new ClusterDB("cluster", hub));         // optional - expose info of hosted databases. Required by Hub Explorer
            hub.EventDispatcher = new EventDispatcher(EventDispatching.QueueSend, env); // optional - enables Pub-Sub (sending events for subscriptions)

            var httpHost        = new HttpHost(hub, "/fliox/", env)       { CacheControl = cache };
            httpHost.AddHandler (new StaticFileHandler(HubExplorer.Path) { CacheControl = cache }); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
        
        private static void AddDatabases(FlioxHub hub, DatabaseSchema schema) {
#if !UNITY_5_3_OR_NEWER
            /* var testDb              = Env.CreateTestDatabase("test_db", Env.TEST_DB_PROVIDER);
            if (testDb != null) {
                await testDb.SeedDatabase(fileDb).ConfigureAwait(false);
                hub.AddExtensionDB (testDb);
            } */
            var sqlite          = $"Data Source={CommonUtils.GetBasePath() + "sqlite_db.sqlite3"}";
            hub.AddExtensionDB  (new SQLiteDatabase     ("sqlite_db",       sqlite,   schema));
            
            var mysql           = EnvConfig.GetConnectionString("mysql");
            hub.AddExtensionDB  (new MySQLDatabase      ("mysql_db",        mysql,    schema));
            
            var mysqlRel        = EnvConfig.GetConnectionString("mysql_rel");
            hub.AddExtensionDB  (new MySQLDatabase      ("mysql_rel",       mysqlRel,schema) { TableType = TableType.Relational });
            
            var mariadb         = EnvConfig.GetConnectionString("mariadb");
            hub.AddExtensionDB  (new MariaDBDatabase    ("maria_db",        mariadb,  schema));
            
            var mariadbRel      = EnvConfig.GetConnectionString("mariadb_rel");
            hub.AddExtensionDB  (new MariaDBDatabase    ("mariadb_rel",     mariadbRel, schema) { TableType = TableType.Relational } );
            
            var postgres        = EnvConfig.GetConnectionString("postgres");
            hub.AddExtensionDB  (new PostgreSQLDatabase ("postgres_db",     postgres, schema));
            
            var sqlServer       = EnvConfig.GetConnectionString("sqlserver");
            hub.AddExtensionDB  (new SQLServerDatabase  ("sqlserver_db",    sqlServer,schema));

            var sqlServerRel    = EnvConfig.GetConnectionString("sqlserver_rel");
            hub.AddExtensionDB  (new SQLServerDatabase  ("sqlserver_rel",   sqlServerRel,schema) { TableType = TableType.Relational } );

            if (UseRedis) {
                var redis           = EnvConfig.GetConnectionString("redis");
                hub.AddExtensionDB  (new RedisHashDatabase  ("redis_db",        redis,    schema));
            }
#endif
        }
        
        private static bool UseRedis = false;
        
        public static async Task DropDatabases() {
            var hub = new FlioxHub(new MemoryDatabase("main"));
            AddDatabases(hub, Schema);
            await DropDatabase(hub, null);
        }
        
        internal static async Task<List<string>> DropDatabase(FlioxHub hub, string dbName)
        {
            IEnumerable<string> databases = (dbName == null) ? hub.GetDatabases().Keys : new [] { dbName };
            var result = new List<string>();
            foreach (var name in databases) {
                if (!hub.TryGetDatabase(name, out var database)) {
                    var msg = $"drop database '{name}' error: database not found";
                    result.Add(msg);
                    continue;
                }
                try {
                    await database.DropDatabaseAsync();
                    var msg = $"drop database '{name}' ({database.StorageType}) successful";
                    result.Add(msg);
                    Console.WriteLine(msg);
                }
                catch (Exception e) {
                    var msg = $"drop database '{name}' ({database.StorageType}) error: {e.Message}";
                    result.Add(msg);
                    Console.WriteLine(msg);
                }
            }
            return result;
        }
    }
    
    public class TestDBCommands : IServiceCommands
    {
        [CommandHandler("DropDatabase")]
        private async Task<Result<List<string>>> DropDatabase(Param<string> param, MessageContext context)
        {
            if (!param.Validate(out var error)) {
                return Result.Error(error);
            }
            return await Program.DropDatabase(context.Hub, param.Value);
        }
    }
}