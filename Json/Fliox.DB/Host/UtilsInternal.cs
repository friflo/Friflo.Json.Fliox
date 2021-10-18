// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Host.Internal;


namespace Friflo.Json.Fliox.DB.Host
{
    public static class UtilsInternal
    {
        public   static readonly    Pools   SharedPools = new Pools(Default.Constructor);
    }
}