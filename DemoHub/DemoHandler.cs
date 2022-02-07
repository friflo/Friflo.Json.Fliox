using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// Implementation of custom database commands declared by <see cref="DemoStore"/>. <br/>
    /// Every method using <see cref="Command{TValue}"/> as parameter is added as a command handler.
    /// Their method names need to match the commands declared in <see cref="DemoStore"/>.
    /// </summary> 
    public class DemoHandler : TaskHandler
    {
        internal DemoHandler() {
            AddCommandHandlers();
        }
        
        /// synchronous command handler
        private static double TestAdd(Command<Operands> command) {
            var param = command.Value;
            return param.left + param.right;
        }
        
        /// asynchronous command handler
        private static Task<double> TestMul(Command<Operands> command) {
            var param = command.Value;
            var result = param.left * param.right;
            return Task.FromResult(result);
        }

        /* private static double TestSub(Command<Operands> command) {
            var param = command.Value;
            return param.left - param.right;
        } */
    }
}