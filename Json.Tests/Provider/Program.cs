using System.Net;
using System.Threading.Tasks;
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
        
        private static async Task<HttpHost> CreateHttpHost() {
            var env                 = new SharedEnv();
            string      cache       = null;
            var schema              = DatabaseSchema.Create<TestClient>();
            var fileDb              = new FileDatabase("file_db", Env.TestDbFolder, schema);
            var memoryDb            = new MemoryDatabase("memory_db", schema);
            await memoryDb.SeedDatabase(fileDb).ConfigureAwait(false);
            
            var hub                 = new FlioxHub(memoryDb, env);
            hub.Info.projectName    = "Test DB";
            hub.Info.projectWebsite = "https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Provider";
            hub.Info.envName        = "test"; hub.Info.envColor = "rgb(0 140 255)";
            hub.AddExtensionDB (fileDb);
#if !UNITY_5_3_OR_NEWER
            /* var testDb              = Env.CreateTestDatabase("test_db", Env.TEST_DB_PROVIDER);
            if (testDb != null) {
                await testDb.SeedDatabase(fileDb).ConfigureAwait(false);
                hub.AddExtensionDB (testDb);
            } */
            var sqlitePath          = CommonUtils.GetBasePath() + "sqlite_db.sqlite3";
            hub.AddExtensionDB      (new SQLiteDatabase("sqlite_db",        sqlitePath,         schema));
            
            var mysqlConnection     = EnvConfig.GetConnectionString("mysql");
            hub.AddExtensionDB      (new MySQLDatabase("mysql_db",          mysqlConnection,    schema));
            
            var mariadbConnection   = EnvConfig.GetConnectionString("mariadb");
            hub.AddExtensionDB      (new MariaDBDatabase("maria_db",        mariadbConnection,  schema));
            
            var postgresConnection  = EnvConfig.GetConnectionString("postgres");
            hub.AddExtensionDB      (new PostgreSQLDatabase("postgres_db",  postgresConnection, schema));
            
            var sqlServerConnection = EnvConfig.GetConnectionString("sqlserver");
            hub.AddExtensionDB      (new SQLServerDatabase("sqlserver_db",  sqlServerConnection,schema));

            var redisConnection     = EnvConfig.GetConnectionString("redis");
            hub.AddExtensionDB      (new RedisHashDatabase("redis_db",      redisConnection,    schema));
#endif
            hub.AddExtensionDB       (new ClusterDB("cluster", hub));         // optional - expose info of hosted databases. Required by Hub Explorer
            hub.EventDispatcher     = new EventDispatcher(EventDispatching.QueueSend, env); // optional - enables Pub-Sub (sending events for subscriptions)
            
            var httpHost            = new HttpHost(hub, "/fliox/", env)       { CacheControl = cache };
            httpHost.AddHandler      (new StaticFileHandler(HubExplorer.Path) { CacheControl = cache }); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
    }
}