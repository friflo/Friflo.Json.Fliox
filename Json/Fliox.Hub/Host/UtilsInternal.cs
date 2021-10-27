// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Internal;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// This static class is placed here instead of <see cref="Friflo.Json.Fliox.Hub.Host.Internal"/> to enable
    /// using it in Fliox.Hub unit tests without the need using the namespace above.
    /// The namespace <see cref="Friflo.Json.Fliox.Hub.Host.Internal"/> is for exclusive use of the library.
    /// </summary>
    public static class UtilsInternal
    {
        public   static readonly    IPools   SharedPools = new Pools(Default.Constructor);
    }
}