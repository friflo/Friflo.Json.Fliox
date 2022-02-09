using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// Implementation of custom database commands declared by <see cref="DemoStore"/>. <br/>
    /// By calling <see cref="TaskHandler.AddCommandHandlers"/> every method with a <see cref="Command{TParam}"/>
    /// parameter is added as a command handler. <br/>
    /// Their method names need to match the commands declared in <see cref="DemoStore"/>.
    /// </summary> 
    public class DemoHandler : TaskHandler
    {
        private static readonly FakeUtils FakeUtils = new FakeUtils();
        
        internal DemoHandler() {
            AddCommandHandlers();
        }
        
        /// synchronous command handler - preferred if possible
        private static double DemoAdd(Command<Operands> command) {
            var param = command.Param;
            return param.left + param.right;
        }
        
        /// asynchronous command handler
        private static Task<double> DemoMul(Command<Operands> command) {
            var param = command.Param;
            var result = param.left * param.right;
            return Task.FromResult(result);
        }

        /* intentionally not implemented to demonstrate response behavior
        private static double DemoSub_NotImpl(Command<Operands> command) {
            var param = command.Param;
            return param.left - param.right;
        } */
        
        /// <summary>
        /// <b> Recommendation </b>: Used an async method to enable concurrent execution of <see cref="DemoStore.SyncTasks"/>.
        /// <br/>
        /// <b> Caution </b>: Using a synchronous method would require to <see cref="Task.Wait()"/> on the SyncTasks() call
        /// resulting in worse performance as a worker thread is exclusively blocked by the while method execution.
        /// </summary> 
        private static async Task<FakeResult> DemoFake(Command<Fake> command) {
            var demoStore       = new DemoStore(command.Hub);
            var user            = command.User;
            demoStore.UserId    = user.userId.ToString(); // todo simplify setting user/token
            demoStore.Token     = user.token;
            demoStore.ClientId  = "DemoFake";
            
            var result = FakeUtils.CreateFakes(command.Param);
            
            if (result.orders       != null)    demoStore.orders    .UpsertRange(result.orders);
            if (result.customers    != null)    demoStore.customers .UpsertRange(result.customers);
            if (result.articles     != null)    demoStore.articles  .UpsertRange(result.articles);
            if (result.producers    != null)    demoStore.producers .UpsertRange(result.producers);
            if (result.employees    != null)    demoStore.employees .UpsertRange(result.employees);
            
            await demoStore.SyncTasks();
            
            var counts = new Fake {
                orders      = result.orders?    .Length,
                customers   = result.customers? .Length,
                articles    = result.articles?  .Length,
                producers   = result.producers? .Length,
                employees   = result.employees? .Length,
            };
            result.counts = counts;
            return result;
        }
    }
}