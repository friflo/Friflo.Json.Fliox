using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;

namespace TodoHub;

internal static class  Program
{
    public static void Main()
    {
        var schema      = DatabaseSchema.Create<TodoClient>();
        var database    = new FileDatabase("main_db", "../Test/DB/main_db", schema); // records stored in 'main_db/jobs'
        database.AddCommands(new TodoCommands());
        var hub         = new FlioxHub(database);
        hub.Info.Set ("TodoHub", "dev", "https://github.com/friflo/Fliox.Examples#todo", "rgb(0 171 145)"); // optional
        hub.UseClusterDB(); // required by HubExplorer
        hub.UsePubSub();    // optional - enables Pub-Sub
        // --- create HttpHost
        var httpHost    = new HttpHost(hub, "/fliox/");
        httpHost.UseStaticFiles(HubExplorer.Path); // nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
        HttpServer.RunHost("http://+:8010/", httpHost); // http://127.0.0.1:8010/fliox/
    }
}

public class TodoCommands : IServiceCommands
{
    [CommandHandler]
    private static async Task<Result<int>> ClearCompletedJobs(Param<bool> param, MessageContext context)
    {
        if (!param.Validate(out string error)) {
            return Result.Error(error);
        }
        var client  = new TodoClient(context.Hub); 
        var jobs    = client.jobs.Query(job => job.completed == param.Value);
        await client.SyncTasks();

        client.jobs.DeleteRange(jobs.Result);
        await client.SyncTasks();
        
        return jobs.Result.Count;
    }
}
