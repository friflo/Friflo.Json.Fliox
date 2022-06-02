// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    internal sealed class RefKeyShort : RefKey<short>
    {
        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class RefKeyShortNull : RefKey<short?>
    {
        internal override   bool                IsKeyNull (short? key)       => key == null;
        
        internal override short? IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short? key) {
            return new JsonKey(key);
        }
    }
}