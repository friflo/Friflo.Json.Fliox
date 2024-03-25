// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        public DiscriminatorAttribute (string discriminator, string description = null) { Description = description; }
        public string     Description { get; }
    }

    /// <summary> Register a specific type for a polymorphic class identified with the given <see cref="Discriminant"/> </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PolymorphTypeAttribute : Attribute {
        public PolymorphTypeAttribute (Type instance, string discriminant = null) { Discriminant = discriminant; }
        public string     Discriminant { get; }
    }

    /// <summary> Register a specific type for the attributed interface </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class InstanceTypeAttribute : Attribute {
        public InstanceTypeAttribute (Type instance) {}
    }
    
    /// <summary>
    /// Defines the naming policy used for class fields and properties.<br/>
    /// It can be used to serialize field and property names as <see cref="NamingPolicyType.CamelCase"/> while
    /// using pascal case names in C# code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class NamingPolicyAttribute : Attribute {
        public NamingPolicyAttribute (NamingPolicyType type) { Type = type; }
        
        public NamingPolicyType Type { get; }
    }
    
    /// <summary>
    /// Naming policy used to serialize class fields and properties to JSON.<br/>  
    /// </summary>
    public enum NamingPolicyType
    {
        /// <summary> Fields and properties serialized unchanged </summary>
        Default,
        /// <summary> Fields and properties serialized as <c>camelCase</c> </summary>
        CamelCase,
        /// <summary> Fields and properties serialized as <c>PascalCase</c> </summary>
        PascalCase,
    }
    

    // -------------------------------- field & property attributes ------------------------------
    /// <summary> Serialize the annotated private / internal member as a JSON field </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializeAttribute : Attribute {
        public SerializeAttribute (string name = null) { Name = name; }
        public string       Name        { get; }
    }
    
    /// <summary> Ignore the annotated public member from JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreAttribute : Attribute {
    }
    
    
    // -------------------------------- enum value attributes ------------------------------
    /// <summary> Use a custom name for the annotated enum value for JSON serialization </summary>
    [AttributeUsage(AttributeTargets.Field)]  // enum fields
    public sealed class EnumValueAttribute : Attribute {
        public EnumValueAttribute (string name = null) { Name = name; }
        public string       Name        { get; }
    }
}

#if UNITY_5_3_OR_NEWER

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class RequiredAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed class KeyAttribute : Attribute { }
}

#endif
