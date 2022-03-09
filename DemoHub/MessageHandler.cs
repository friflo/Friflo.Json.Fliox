using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace Fliox.DemoHub
{
    /// <summary>
    /// <see cref="MessageHandler"/> extends <see cref="TaskHandler"/> to implement custom database commands
    /// executed at the host and declared by the <see cref="DemoStore"/>.
    /// <br/>
    /// A <see cref="TaskHandler"/> instance need to be passed when instantiating an <see cref="EntityDatabase"/>. <br/>
    /// E.g. a <see cref="MemoryDatabase"/>, a <see cref="FileDatabase"/>, ... <br/>
    /// <br/>
    /// By calling <see cref="TaskHandler.AddMessageHandlers{TClass}"/> every method with the parameters <br/>
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
            
            if (!param.GetValidate(out var fake, out var error))
                return command.Error<Records>(error);
            
            var result = FakeUtils.CreateFakes(fake);
            
            if (result.orders       != null)    demoStore.orders    .UpsertRange(result.orders);
            if (result.customers    != null)    demoStore.customers .UpsertRange(result.customers);
            if (result.articles     != null)    demoStore.articles  .UpsertRange(result.articles);
            if (result.producers    != null)    demoStore.producers .UpsertRange(result.producers);
            if (result.employees    != null)    demoStore.employees .UpsertRange(result.employees);
            
            await demoStore.SyncTasks();
            
            var addResults  = fake?.addResults;
            if (addResults.HasValue && addResults.Value == false) {
                result.orders       = null;
                result.customers    = null;
                result.articles     = null;
                result.producers    = null;
                result.employees    = null;
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

            var orderCount      = demoStore.orders.     Count(o => o.created >= from);
            var customerCount   = demoStore.customers.  Count(o => o.created >= from);
            var articleCount    = demoStore.articles.   Count(o => o.created >= from);
            var producerCount   = demoStore.producers.  Count(o => o.created >= from);
            var employeeCount   = demoStore.employees.  Count(o => o.created >= from);
            
            await demoStore.SyncTasks();
            
            var result = new Counts {
                orders      = orderCount.     Result,
                customers   = customerCount.  Result,
                articles    = articleCount.   Result,
                producers   = producerCount.  Result,
                employees   = employeeCount.  Result,
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

            var orderCount      = demoStore.orders.     Query(o => o.created >= from);
            var customerCount   = demoStore.customers.  Query(o => o.created >= from);
            var articleCount    = demoStore.articles.   Query(o => o.created >= from);
            var producerCount   = demoStore.producers.  Query(o => o.created >= from);
            var employeeCount   = demoStore.employees.  Query(o => o.created >= from);
            
            await demoStore.SyncTasks();
            
            var counts = new Counts {
                orders      = orderCount.   Result.Count,
                customers   = customerCount.Result.Count,
                articles    = articleCount. Result.Count,
                producers   = producerCount.Result.Count,
                employees   = employeeCount.Result.Count,
            };
            var result = new Records {
                counts      = counts,
                orders      = orderCount.   Result.Values.ToArray(),
                customers   = customerCount.Result.Values.ToArray(),
                articles    = articleCount. Result.Values.ToArray(),
                producers   = producerCount.Result.Values.ToArray(),
                employees   = employeeCount.Result.Values.ToArray(),
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