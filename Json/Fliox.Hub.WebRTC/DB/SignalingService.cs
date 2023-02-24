using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.WebRTC.Remote;
using Friflo.Json.Fliox.Schema.Native;
using SIPSorcery.Net;

namespace Friflo.Json.Fliox.Hub.WebRTC.DB
{
    public class SignalingService : DatabaseService
    {
        private readonly FlioxHub       hub;

        public  static  DatabaseSchema  Schema => GetSchema();
        private static  DatabaseSchema  _schema;

        private const string STUN_URL = "stun:stun.sipsorcery.com";

        public SignalingService(FlioxHub hub) {
            this.hub = hub;
            AddMessageHandlers(this, null);
        }
        
        private static DatabaseSchema GetSchema() {
            if (_schema != null) {
                return _schema;
            }
            return _schema = new DatabaseSchema(typeof(Signaling));
        }
        
        private AddHostResult AddHost (Param<AddHost> param, MessageContext command) {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<AddHostResult>(error);
            }
            RTCConfiguration config = new RTCConfiguration {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = STUN_URL } }
            };
            _ = WebRtcHost.SendReceiveMessages(config, null, hub);
            return new AddHostResult();
        }
    }
}