// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Local
namespace Friflo.Json.Fliox
{
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
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : Attribute {
        public string       Name        { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoreMemberAttribute : Attribute {
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredMemberAttribute : Attribute {
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute {
    }
    
    [AttributeUsage(AttributeTargets.Field)]  // enum fields
    public sealed class EnumValueAttribute : Attribute {
        public string       Name        { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RelationAttribute : Attribute {
        public RelationAttribute (string instance) {}
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessagePrefixAttribute : Attribute {
        public MessagePrefixAttribute (string prefix) { }
    }
    
    // ------------------------------- OpenAPI attributes -------------------------------
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#openapi-object">OpenAPI Object specification</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OpenAPI : Attribute {
        public string           Version        { get; set; }
        public string           TermsOfService { get; set; }
        
        public string           LicenseName    { get; set; }
        public string           LicenseUrl     { get; set; }
        
        public string           ContactName    { get; set; }
        public string           ContactUrl     { get; set; }
        public string           ContactEmail   { get; set; }
    }
    
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#server-object">OpenAPI Server Object</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OpenAPIServer : Attribute {
        public string           Url             { get; set; }
        public string           Description     { get; set; }
    }

}
