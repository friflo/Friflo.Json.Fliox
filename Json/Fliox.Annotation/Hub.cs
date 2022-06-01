// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Fliox
{
    // --- Database Attributes - used by: Friflo.Json.Fliox.Hub 
    
    // -------------------------------- class attributes ------------------------------
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessagePrefixAttribute : Attribute {
        public MessagePrefixAttribute (string prefix) { }
    }
    
    // -------------------------------- field & property attributes ------------------------------
    /* /// <summary> Declare the attributed member is the primary key of an entity in its container </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute {  } */

    /// <summary> Declare the attributed member as an auto increment field / property </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute {
    }
    
    /// <summary> Specify that the attributed member is a reference (secondary key) to an entity in the given container </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RelationAttribute : Attribute {
        public RelationAttribute (string container) {}
    }
    
    // -------------------------------- method attributes ------------------------------
    /// <summary> Set a custom command name for the attributed method </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DatabaseCommandAttribute : Attribute {
        public string       Name        { get; set; }
    }
}