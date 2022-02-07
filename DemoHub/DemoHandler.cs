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
        private static double TestAdd(Command<Operands> command) {
            var param = command.Param;
            return param.left + param.right;
        }
        
        /// asynchronous command handler
        private static Task<double> TestMul(Command<Operands> command) {
            var param = command.Param;
            var result = param.left * param.right;
            return Task.FromResult(result);
        }

        /* intentionally not implemented to demonstrate response behavior
        private static double TestSub_NotImpl(Command<Operands> command) {
            var param = command.Param;
            return param.left - param.right;
        } */
    }
}