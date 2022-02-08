using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

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
        
        private static async Task<FakeResult> DemoFake(Command<Fake> command) {
            var demoStore       = new DemoStore(command.Hub);
            var user            = command.User;
            demoStore.UserId    = user.userId.ToString(); // todo simplify setting user/token
            demoStore.Token     = user.token;
            // demoStore.ClientId  = command.ClientId.ToString();
            var article         = new Article { id = "test", name = "foo" };
            demoStore.articles.Upsert(article);
            await demoStore.SyncTasks();
            
            var result          = new FakeResult();
            result.articles = new [] { article };
            return result;
        }
    }
}