using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.DemoHub
{
    public class DemoHandler : TaskHandler {
        public DemoHandler() {
            AddCommandHandler<TestAdd, double>(nameof(TestAdd), TestAdd);
        }
        
        private static double TestAdd(Command<TestAdd> command) {
            var param = command.Value;
            return param.left * param.right;
        }
    }
}