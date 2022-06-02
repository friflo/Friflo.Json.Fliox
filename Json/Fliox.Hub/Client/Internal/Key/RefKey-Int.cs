// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class RefKeyInt : RefKey<int>
    {
        internal override int IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyIntNull : RefKey<int?>
    {
        internal override   bool                IsKeyNull (int? key)       => key == null;
        
        internal override int? IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int? key) {
            return new JsonKey(key);
        }
    }
}