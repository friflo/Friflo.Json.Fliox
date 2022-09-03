
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public class HubPermission
    {
        public readonly bool queueEvents;

        public static readonly HubPermission Full = new HubPermission (true);
        public static readonly HubPermission None = new HubPermission (false);
        
        public HubPermission(bool queueEvents) {
            this.queueEvents = queueEvents;
        }
    }
}