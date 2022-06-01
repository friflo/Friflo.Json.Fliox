// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    // -------------------------------- class & interface attributes ------------------------------
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TypeMapperAttribute : Attribute {
        public TypeMapperAttribute (Type typeMapper) {}
    }
    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class DiscriminatorAttribute : Attribute {
        public DiscriminatorAttribute (string discriminator) {}
        public string     Description    { get; set; }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PolymorphTypeAttribute : Attribute {
        public string     Discriminant    { get; set; }
        public PolymorphTypeAttribute (Type instance) {}
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class InstanceTypeAttribute : Attribute {
        public InstanceTypeAttribute (Type instance) {}
    }
    

    // -------------------------------- field & property attributes ------------------------------
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PropertyMemberAttribute : Attribute {
        public string       Name        { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreMemberAttribute : Attribute {
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredMemberAttribute : Attribute {
    }
    
    
    // -------------------------------- enum value attributes ------------------------------
    [AttributeUsage(AttributeTargets.Field)]  // enum fields
    public sealed class EnumValueAttribute : Attribute {
        public string       Name        { get; set; }
    }
}
