// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using SIPSorcery.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class SignalingService : DatabaseService
    {
        private readonly    FlioxHub            hub;
        private readonly    WebRtcConfig        config;

        public  static      DatabaseSchema      Schema => GetSchema();
        private static      DatabaseSchema      _schema;

        public SignalingService(FlioxHub hub, WebRtcConfig config) {
            this.hub    = hub;
            this.config = config;
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
            _ = WebRtcHost.SendReceiveMessages(config, null, hub);
            return new AddHostResult();
        }
    }
}