using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.DemoHub
{
    public class DemoHandler : TaskHandler {
        public DemoHandler() {
            AddCommandHandler<TestCommand, bool>(nameof(TestCommand), TestCommand);
        }
        
        private static bool TestCommand(Command<TestCommand> command) {
            return true;
        }
    }
}