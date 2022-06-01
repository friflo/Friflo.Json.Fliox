// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    // -------------------------------- class attributes ------------------------------
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessagePrefixAttribute : Attribute {
        public MessagePrefixAttribute (string prefix) { }
    }
    
    // -------------------------------- field & property attributes ------------------------------
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute {
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ContainerRelationAttribute : Attribute {
        public ContainerRelationAttribute (string instance) {}
    }
    
    // -------------------------------- method attributes ------------------------------
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DatabaseCommandAttribute : Attribute {
        public string       Name        { get; set; }
    }
}