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
        private static double TestAdd(Command<Add> command) {
            var param = command.Value;
            return param.left + param.right;
        }

    /*  private static double TestSub(Command<Sub> command) {
            var param = command.Value;
            return param.left - param.right;
        } */
    }
}