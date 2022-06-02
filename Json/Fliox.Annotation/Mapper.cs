// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Local
namespace Friflo.Json.Fliox
{
    // --- serialization Attributes
    // used by Friflo.Json.Fliox.Mapper (ObjectMapper) for POCO's
    
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
    public sealed class SerializeAttribute : Attribute {
        public string       Name        { get; set; }
    }
    
    /// <summary> Used to ignore the annotated public member from JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreAttribute : Attribute {
    }
    
    
    // -------------------------------- enum value attributes ------------------------------
    /// <summary> Use a custom name for the annotated enum value for JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field)]  // enum fields
    public sealed class EnumValueAttribute : Attribute {
        public string       Name        { get; set; }
    }
}

#if UNITY_5_3_OR_NEWER

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class RequiredAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public class KeyAttribute : Attribute { }
}

#endif
