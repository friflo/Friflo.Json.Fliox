// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    /// Singleton are typically a bad practice, but its okay in this case as <see cref="TypeStore"/> behaves like an
    /// immutable object because the mapped types <see cref="SyncRequest"/> and <see cref="SyncResponse"/> are
    /// a fixed set of types. 
    public static class SyncTypeStore
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
        
        private static TypeStore Get() {
            if (_singleton == null) {
                _singleton = new TypeStore();
                _singleton.GetTypeMapper(typeof(DatabaseRequest));
                _singleton.GetTypeMapper(typeof(DatabaseResponse));
                _singleton.GetTypeMapper(typeof(DatabaseMessage));
                _singleton.GetTypeMapper(typeof(ErrorResponse));
                _singleton.GetTypeMapper(typeof(ValidateToken));
            }
            return _singleton;
        }

        /// <summary> Returned <see cref="ObjectMapper"/> doesnt throw Read() exceptions. To handle errors its
        /// <see cref="ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        internal static ObjectMapper CreateObjectMapper() {
            var mapper = new ObjectMapper(Get(), new NoThrowHandler());
            return mapper;
        }
    }
}