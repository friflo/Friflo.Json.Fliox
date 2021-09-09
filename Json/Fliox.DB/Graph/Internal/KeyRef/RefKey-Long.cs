// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.KeyRef
{
    internal class RefKeyLong<T> : RefKey<long, T> where T : class
    {
        internal override long IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long key) {
            return new JsonKey(key);
        }
    }
    
    internal class RefKeyLongNull<T> : RefKey<long?, T> where T : class
    {
        internal override   bool                IsKeyNull (long? key)       => key == null;
        
        internal override long? IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long? key) {
            return new JsonKey(key);
        }
    }
}