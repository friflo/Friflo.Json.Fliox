// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;

namespace Friflo.Json.Managed.Prop
{
    public interface IPropDriver
    {
        PropField CreateVariable (PropType declType, String name, FieldInfo field);
    }
}
