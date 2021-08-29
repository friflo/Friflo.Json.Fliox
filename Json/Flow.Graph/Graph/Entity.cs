// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using Friflo.Json.Flow.Mapper;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Graph
{
    /// <summary>
    /// <see cref="Entity"/> can be used as a base class for entity model classes.<br/>
    /// Doing this is optional. If its used it provides the features listed below.
    /// <list type="bullet">
    ///   <item>Enable instant listing of all declared entity models by using IDE tools like: "Find usage" or "Find All References"</item>
    ///   <item>Ensures an entity key (id) is not changed when already assigned by a runtime assertion.</item>
    /// </list>  
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class Entity
    {
        [Fri.Required]
        public  string  id {
            get => _id;
            set {
                if (_id == value)
                    return;
                if (_id == null) {
                    _id = value;
                    return;
                }
                throw new ArgumentException($"Entity id must not be changed. Type: {GetType().Name}, was: '{_id}', assigned: '{value}'");
            }
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string  _id;

        public override     string  ToString() => id ?? "null";
    }
}