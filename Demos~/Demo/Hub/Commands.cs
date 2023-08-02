using System;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace DemoHub;

/// <summary>
/// <see cref="DemoCommands"/> extends <see cref="IServiceCommands"/> to handle custom commands send by a <see cref="DemoClient"/>.
/// <br/>
/// Database commands are executed at the host and declared by the <see cref="DemoClient"/>. <br/>
/// Therefore it create <see cref="DemoClient"/> clients in its handler methods to perform database operations
/// like query, count and upsert.<br/>
/// <br/>
/// <see cref="IServiceCommands"/> are added to a database using <see cref="EntityDatabase.AddCommands"/>. <br/>
/// E.g. a <see cref="MemoryDatabase"/>, a <see cref="FileDatabase"/>, ... <br/>
/// <br/>
/// Every method attributed with <see cref="CommandHandlerAttribute"/> handle commands sent to the service<br/>
/// To handle messages a method need to be attributed with <see cref="MessageHandlerAttribute"/>.
/// </summary>
public class DemoCommands : IServiceCommands
{
    private static readonly FakeUtils FakeUtils = new FakeUtils();
    
    /// <summary>
    /// <b> Recommendation </b>: Used an async method to enable concurrent execution of demoStore.SyncTasks()/>.
    /// <br/>
    /// <b> Note </b>: Using a synchronous method would require to <see cref="Task.Wait()"/> on the SyncTasks() call
    /// resulting in worse performance as a worker thread is exclusively blocked by the while method execution.
    /// </summary>
    [CommandHandler("demo.FakeRecords")]
    private static async Task<Result<Records>> FakeRecords(Param<Fake> param, MessageContext context)
    {
        var client      = new DemoClient(context.Hub);
        client.UserInfo = context.UserInfo;
        client.Options.WritePretty  = true;
        
        if (!param.GetValidate(out var fake, out var error)) {
            return Result.ValidationError(error);
        }
        var result = FakeUtils.CreateFakes(fake);
        
        if (result.articles     != null)    client.articles  .UpsertRange(result.articles);
        if (result.customers    != null)    client.customers .UpsertRange(result.customers);
        if (result.employees    != null)    client.employees .UpsertRange(result.employees);
        if (result.orders       != null)    client.orders    .UpsertRange(result.orders);
        if (result.producers    != null)    client.producers .UpsertRange(result.producers);
        
        await client.SyncTasks();
        
        var addResults  = fake?.addResults;
        if (addResults.HasValue && addResults.Value == false) {
            result.articles     = null;
            result.customers    = null;
            result.employees    = null;
            result.orders       = null;
            result.producers    = null;
        }
        return result;
    }

    [CommandHandler("demo.CountLatest")]
    private static async Task<Result<Counts>> CountLatest(Param<int?> param, MessageContext context)
    {
        var client      = new DemoClient(context.Hub);
        client.UserInfo = context.UserInfo;
        
        if (!param.GetValidate(out var duration, out var error)) {
            return Result.ValidationError(error);
        }
        
        var seconds         = duration ?? 60;
        var from            = DateTime.Now.AddSeconds(-seconds);

        var articleCount    = client.articles.   Count(o => o.created >= from);
        var customerCount   = client.customers.  Count(o => o.created >= from);
        var employeeCount   = client.employees.  Count(o => o.created >= from);
        var orderCount      = client.orders.     Count(o => o.created >= from);
        var producerCount   = client.producers.  Count(o => o.created >= from);
        
        await client.SyncTasks();
        
        var result = new Counts {
            articles    = articleCount.   Result,
            customers   = customerCount.  Result,
            employees   = employeeCount.  Result,
            orders      = orderCount.     Result,
            producers   = producerCount.  Result,
        };
        return result;
    }
    
    [CommandHandler("demo.LatestRecords")]
    private static async Task<Result<Records>> LatestRecords(Param<int?> param, MessageContext context)
    {
        var client      = new DemoClient(context.Hub);
        client.UserInfo = context.UserInfo;
        
        if (!param.GetValidate(out var duration, out var error)) {
            return Result.ValidationError(error);
        }
        var seconds         = duration ?? 60;
        var from            = DateTime.Now.AddSeconds(-seconds);

        var articleCount    = client.articles.   Query(o => o.created >= from);
        var customerCount   = client.customers.  Query(o => o.created >= from);
        var employeeCount   = client.employees.  Query(o => o.created >= from);
        var orderCount      = client.orders.     Query(o => o.created >= from);
        var producerCount   = client.producers.  Query(o => o.created >= from);
        
        await client.SyncTasks();
        
        var counts = new Counts {
            articles    = articleCount. Result.Count,
            customers   = customerCount.Result.Count,
            employees   = employeeCount.Result.Count,
            orders      = orderCount.   Result.Count,
            producers   = producerCount.Result.Count,
        };
        var result = new Records {
            counts      = counts,
            articles    = articleCount. Result.ToArray(),
            customers   = customerCount.Result.ToArray(),
            employees   = employeeCount.Result.ToArray(),
            orders      = orderCount.   Result.ToArray(),
            producers   = producerCount.Result.ToArray(),
        };
        return result;
    }
    
    /// use synchronous handler only when no async methods need to be awaited
    [CommandHandler("demo.Add")]
    private static Result<double> Add(Param<Operands> param, MessageContext context)
    {
        if (!param.GetValidate(out var operands, out var error)) {
            return Result.ValidationError(error);
        }
        if (operands == null)
            return 0;
        return operands.left + operands.right;
    }
}
