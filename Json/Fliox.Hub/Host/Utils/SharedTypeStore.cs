// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host.Monitor;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.UserAuth;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    /// Singleton are typically a bad practice, but its okay in this case as <see cref="TypeStore"/> behaves like an
    /// immutable object because the mapped types <see cref="SyncRequest"/> and <see cref="SyncResponse"/> are
    /// a fixed set of types. 
    public static class SharedTypeStore
    {
        private static TypeStore _singleton;

        public static void Init() {
            Get();
        }
        
        public static void Dispose() {
            var s = _singleton;
            if (s == null)
                return;
            _singleton = null;
            s.Dispose();
        }
        
        public static void AddHostTypes(TypeStore typeStore) {
            typeStore.GetTypeMapper(typeof(ProtocolMessage));
            typeStore.GetTypeMapper(typeof(UserStore));
            typeStore.GetTypeMapper(typeof(RemoteSubscriptionEvent));
            typeStore.GetTypeMapper(typeof(MonitorStore));
        }
        
        internal static TypeStore Get() {
            if (_singleton == null) {
                _singleton = new TypeStore();
                AddHostTypes(_singleton);
            }
            return _singleton;
        }
    }
}