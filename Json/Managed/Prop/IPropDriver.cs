// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Prop
{
    public interface IPropDriver
    {
        PropField CreateVariable (TypeResolver resolver, PropType declType, String name, FieldInfo field);
    }
}
