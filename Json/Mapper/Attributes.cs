// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class JsonTypeAttribute : Attribute {
        public Type     TypeMapper    { get; set; }
    }
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class DiscriminatorAttribute : Attribute {
        public DiscriminatorAttribute (string discriminator) {}
    }
    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PolymorphAttribute : Attribute {
        public string     Discriminant    { get; set; }
        public PolymorphAttribute (Type instance) {}
    }
    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class InstanceAttribute : Attribute {
        public InstanceAttribute (Type instance) {}
    }
}
