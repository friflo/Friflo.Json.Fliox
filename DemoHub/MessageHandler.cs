using System;
using System.Threading.Tasks;
using DemoClient;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace DemoHub
{
    /// <summary>
    /// <see cref="MessageHandler"/> extends <see cref="TaskHandler"/> to implement the <see cref="DemoStore"/> API (database commands).
    /// <br/>
    /// Database commands are executed at the host and declared by the <see cref="DemoStore"/>. <br/>
    /// Therefore it create <see cref="DemoStore"/> clients in its handler methods to perform database operations
    /// like query, count and upsert.<br/>
    /// <br/>
    /// A <see cref="TaskHandler"/> instance need to be passed when instantiating an <see cref="EntityDatabase"/>. <br/>
    /// E.g. a <see cref="MemoryDatabase"/>, a <see cref="FileDatabase"/>, ... <br/>
    /// <br/>
    /// By calling <see cref="TaskHandler.AddMessageHandlers{TClass}"/> every method with the signature <br/>
    /// (<see cref="Param{TParam}"/> param, <see cref="MessageContext"/> context) is added as a command handler. <br/>
    /// Their method names need to match the command methods declared in the <see cref="DemoStore"/>.
    /// </summary>
    public class MessageHandler : TaskHandler
    {
        private static readonly Utils FakeUtils = new Utils();
        
        internal MessageHandler() {
            AddMessageHandlers(this, "demo.");
        }
        
        /// <summary>
        /// <b> Recommendation </b>: Used an async method to enable concurrent execution of demoStore.SyncTasks()/>.
        /// <br/>
        /// <b> Note </b>: Using a synchronous method would require to <see cref="Task.Wait()"/> on the SyncTasks() call
        /// resulting in worse performance as a worker thread is exclusively blocked by the while method execution.
        /// </summary> 
        private static async Task<Records> FakeRecords(Param<Fake> param, MessageContext command) {
            var demoStore       = new DemoStore(command.Hub);
            demoStore.UserInfo  = command.UserInfo;
            demoStore.WritePretty = true;
            
            if (!param.GetValidate(out var fake, out var error))
                return command.Error<Records>(error);
            
            var result = FakeUtils.CreateFakes(fake);
            
            if (result.articles     != null)    demoStore.articles  .UpsertRange(result.articles);
            if (result.customers    != null)    demoStore.customers .UpsertRange(result.customers);
            if (result.employees    != null)    demoStore.employees .UpsertRange(result.employees);
            if (result.orders       != null)    demoStore.orders    .UpsertRange(result.orders);
            if (result.producers    != null)    demoStore.producers .UpsertRange(result.producers);
            
            await demoStore.SyncTasks();
            
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

        private static async Task<Counts> CountLatest(Param<int?> param, MessageContext command) {
            var demoStore       = new DemoStore(command.Hub);
            demoStore.UserInfo  = command.UserInfo;
            
            if (!param.GetValidate(out var duration, out var error))
                return command.Error<Counts>(error);
            
            var seconds         = duration ?? 60;
            var from            = DateTime.Now.AddSeconds(-seconds);

            var articleCount    = demoStore.articles.   Count(o => o.created >= from);
            var customerCount   = demoStore.customers.  Count(o => o.created >= from);
            var employeeCount   = demoStore.employees.  Count(o => o.created >= from);
            var orderCount      = demoStore.orders.     Count(o => o.created >= from);
            var producerCount   = demoStore.producers.  Count(o => o.created >= from);
            
            await demoStore.SyncTasks();
            
            var result = new Counts {
                articles    = articleCount.   Result,
                customers   = customerCount.  Result,
                employees   = employeeCount.  Result,
                orders      = orderCount.     Result,
                producers   = producerCount.  Result,
            };
            return result;
        }
        
        private static async Task<Records> LatestRecords(Param<int?> param, MessageContext command) {
            var demoStore       = new DemoStore(command.Hub);
            demoStore.UserInfo  = command.UserInfo;
            
            if (!param.GetValidate(out var duration, out var error))
                return command.Error<Records>(error);
            
            var seconds         = duration ?? 60;
            var from            = DateTime.Now.AddSeconds(-seconds);

            var articleCount    = demoStore.articles.   Query(o => o.created >= from);
            var customerCount   = demoStore.customers.  Query(o => o.created >= from);
            var employeeCount   = demoStore.employees.  Query(o => o.created >= from);
            var orderCount      = demoStore.orders.     Query(o => o.created >= from);
            var producerCount   = demoStore.producers.  Query(o => o.created >= from);
            
            await demoStore.SyncTasks();
            
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
        private static double Add(Param<Operands> param, MessageContext command) {
            if (!param.GetValidate(out var operands, out var error))
                return command.Error<double>(error);
            if (operands == null)
                return 0;
            return operands.left + operands.right;
        }
    }
}