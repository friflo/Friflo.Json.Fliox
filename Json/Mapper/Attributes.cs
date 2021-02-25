// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class FloTypeMapperAttribute : Attribute {
        public FloTypeMapperAttribute (Type  typeMapper) {}
    }
    
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class FloDiscriminatorAttribute : Attribute {
        public FloDiscriminatorAttribute (string discriminator) {}
    }
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class FloPolymorphAttribute : Attribute {
        public string     Discriminant    { get; set; }
        public FloPolymorphAttribute (Type instance) {}
    }
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class FloInstanceAttribute : Attribute {
        public FloInstanceAttribute (Type instance) {}
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class FloPropertyAttribute : Attribute {
        public string     Name    { get; set; }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class FloIgnoreAttribute : Attribute {
    }
}
