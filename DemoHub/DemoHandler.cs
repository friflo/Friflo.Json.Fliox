using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.DemoHub
{
    public class DemoHandler : TaskHandler {
        public DemoHandler() {
            AddCommandHandler<TestAdd, double>(nameof(TestAdd), TestAdd);
            // AddCommandHandler<TestSub, double>(nameof(TestSub), TestSub);
        }
        
        private static double TestAdd(Command<TestAdd> command) {
            var param = command.Value;
            return param.left + param.right;
        }
        
        /*
        private static double TestSub(Command<TestSub> command) {
            var param = command.Value;
            return param.left - param.right;
        } */
    }
}