using System;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace DemoHub
{
    /// <summary>
    /// <see cref="DemoService"/> extends <see cref="DatabaseService"/> to implement the <see cref="DemoClient"/> API (database commands).
    /// <br/>
    /// Database commands are executed at the host and declared by the <see cref="DemoClient"/>. <br/>
    /// Therefore it create <see cref="DemoClient"/> clients in its handler methods to perform database operations
    /// like query, count and upsert.<br/>
    /// <br/>
    /// A <see cref="DatabaseService"/> instance need to be passed when instantiating an <see cref="EntityDatabase"/>. <br/>
    /// E.g. a <see cref="MemoryDatabase"/>, a <see cref="FileDatabase"/>, ... <br/>
    /// <br/>
    /// By calling <see cref="DatabaseService.AddMessageHandlers{TClass}"/> every method with the signature <br/>
    /// (<see cref="Param{TParam}"/> param, <see cref="MessageContext"/> context) is added as a command handler. <br/>
    /// Their method names need to match the command methods declared in the <see cref="DemoClient"/>.
    /// </summary>
    public class DemoService : DatabaseService
    {
        private static readonly FakeUtils FakeUtils = new FakeUtils();
        
        public DemoService()
        {
            AddMessageHandlers(this, "demo.");
        }
        
        /// <summary>
        /// <b> Recommendation </b>: Used an async method to enable concurrent execution of demoStore.SyncTasks()/>.
        /// <br/>
        /// <b> Note </b>: Using a synchronous method would require to <see cref="Task.Wait()"/> on the SyncTasks() call
        /// resulting in worse performance as a worker thread is exclusively blocked by the while method execution.
        /// </summary> 
        private static async Task<Records> FakeRecords(Param<Fake> param, MessageContext command)
        {
            var client          = new DemoClient(command.Hub);
            client.UserInfo     = command.UserInfo;
            client.WritePretty  = true;
            
            if (!param.GetValidate(out var fake, out var error)) {
                command.ValidationError(error);
                return null;
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

        private static async Task<Counts> CountLatest(Param<int?> param, MessageContext command)
        {
            var client      = new DemoClient(command.Hub);
            client.UserInfo = command.UserInfo;
            
            if (!param.GetValidate(out var duration, out var error)) {
                command.ValidationError(error);
                return null;
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
        
        private static async Task<Records> LatestRecords(Param<int?> param, MessageContext command)
        {
            var client      = new DemoClient(command.Hub);
            client.UserInfo = command.UserInfo;
            
            if (!param.GetValidate(out var duration, out var error)) {
                command.ValidationError(error);
                return null;
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
        private static double Add(Param<Operands> param, MessageContext command)
        {
            if (!param.GetValidate(out var operands, out var error)) {
                command.ValidationError(error);
                return 0;
            }
            if (operands == null)
                return 0;
            return operands.left + operands.right;
        }
    }
}