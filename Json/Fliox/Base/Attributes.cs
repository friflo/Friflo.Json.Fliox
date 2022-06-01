// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    // -------------------------------- class & interface attributes ------------------------------
    /// <summary> Register a custom TypeMapper for the attributed class, interface or struct </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TypeMapperAttribute : Attribute {
        public TypeMapperAttribute (Type typeMapper) {}
    }
    
    /// <summary> Declare the field / property <b>name</b> acting as discriminator for a polymorph class or interface</summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class DiscriminatorAttribute : Attribute {
        public DiscriminatorAttribute (string discriminator) {}
        public string     Description    { get; set; }
    }

    /// <summary> Register a specific type for a polymorphic class identified with the given <see cref="Discriminant"/> </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PolymorphTypeAttribute : Attribute {
        public string     Discriminant    { get; set; }
        public PolymorphTypeAttribute (Type instance) {}
    }

    /// <summary> Register a specific type for the attributed interface </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class InstanceTypeAttribute : Attribute {
        public InstanceTypeAttribute (Type instance) {}
    }
    

    // -------------------------------- field & property attributes ------------------------------
    /// <summary> Used serialize the attributed private / internal member as a JSON field </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializeFieldAttribute : Attribute {
        public string       Name        { get; set; }
    }
    
    /// <summary> Used to ignore the annotated public member from JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreFieldAttribute : Attribute {
    }
    
    /// <summary> Declare the attributed member as a required field </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredFieldAttribute : Attribute {
    }
    
    
    // -------------------------------- enum value attributes ------------------------------
    /// <summary> Use a custom name for the annotated enum value for JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field)]  // enum fields
    public sealed class EnumValueAttribute : Attribute {
        public string       Name        { get; set; }
    }
}
