// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper
{
    public static class Fri {
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class TypeMapperAttribute : Attribute {
            public TypeMapperAttribute (Type  typeMapper) {}
        }
        
        
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class DiscriminatorAttribute : Attribute {
            public DiscriminatorAttribute (string discriminator) {}
        }
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public sealed class PolymorphAttribute : Attribute {
            public string     Discriminant    { get; set; }
            public PolymorphAttribute (Type instance) {}
        }
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class InstanceAttribute : Attribute {
            public InstanceAttribute (Type instance) {}
        }
        
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class PropertyAttribute : Attribute {
            public string     Name    { get; set; }
        }
        
    #if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
    #endif
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class IgnoreAttribute : Attribute {
        }
    }
}
