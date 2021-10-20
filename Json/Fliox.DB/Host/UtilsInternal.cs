// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Host.Internal;


namespace Friflo.Json.Fliox.DB.Host
{
    /// <summary>
    /// This static class is placed her instead of <see cref="Friflo.Json.Fliox.DB.Host.Internal"/> to enable
    /// using it in Fliox.DB unit tests without the need using the namespace above.
    /// This namespace is mainly required only if implementing an <see cref="DatabaseHub"/>.
    /// </summary>
    public static class UtilsInternal
    {
        public   static readonly    IPools   SharedPools = new Pools(Default.Constructor);
    }
}