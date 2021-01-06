// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Burst;

namespace Friflo.Json.Managed.Prop
{
    public interface ITypelessKeySet
    {
        IProperties GetEntry(Bytes key);
    }

    public interface IKeySet<T> : ITypelessKeySet where T: IProperties
    {
    }


}
