using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// Implementation of database commands declared by <see cref="DemoStore"/>.
    /// </summary> 
    public class DemoHandler : TaskHandler
    {
        public DemoHandler() {
            AddCommandHandler<Add, double>(nameof(TestAdd), TestAdd);
        //  AddCommandHandler<Sub, double>(nameof(TestSub), TestSub);
        }
        
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