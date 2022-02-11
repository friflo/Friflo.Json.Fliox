using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// A <see cref="TaskHandler"/> is required to implement custom database commands declared by <see cref="DemoStore"/>.
    /// <br/>
    /// An instance of this class need to be passed when instantiating an <see cref="EntityDatabase"/> implementation.
    /// E.g. a <see cref="MemoryDatabase"/>, a <see cref="FileDatabase"/>, ... <br/>
    /// By calling <see cref="TaskHandler.AddCommandHandlers{TClass}"/> every method with a <see cref="Command{TParam}"/>
    /// parameter is added as a command handler. <br/>
    /// Their method names need to match the commands declared in <see cref="DemoStore"/>.
    /// </summary> 
    public class DemoHandler : TaskHandler
    {
        private static readonly FakeUtils FakeUtils = new FakeUtils();
        
        internal DemoHandler() {
            AddCommandHandlers(this, null);
        }
        
        /// synchronous command handler - preferred if possible
        private static double Add(Command<Operands> command) {
            var param = command.Param;
            return param.left + param.right;
        }
        
        /// asynchronous command handler
        private static Task<double> Mul(Command<Operands> command) {
            var param = command.Param;
            var result = param.left * param.right;
            return Task.FromResult(result);
        }

        /* intentionally not implemented to demonstrate response behavior
        private static double Sub_NotImpl(Command<Operands> command) {
            var param = command.Param;
            return param.left - param.right;
        } */
        
        /// <summary>
        /// <b> Recommendation </b>: Used an async method to enable concurrent execution of <see cref="DemoStore.SyncTasks"/>.
        /// <br/>
        /// <b> Caution </b>: Using a synchronous method would require to <see cref="Task.Wait()"/> on the SyncTasks() call
        /// resulting in worse performance as a worker thread is exclusively blocked by the while method execution.
        /// </summary> 
        private static async Task<FakeResult> Fake(Command<Fake> command) {
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
            
            return result;
        }
    }
}