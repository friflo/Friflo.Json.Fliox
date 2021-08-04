// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Definition
{
    /// <summary>
    /// <see cref="TypeSchema"/> is an abstraction of an entire type system / schema used as input for code generators
    /// and JSON payload validation like <b>JSON Schema</b>.
    /// <br></br>
    /// The abstraction enables:
    /// <list type="bullet">
    ///   <item>
    ///     Simplify implementation of code generators as its API is tailored towards retrieving type information by
    ///     simple getters using <see cref="TypeDef"/>, <see cref="FieldDef"/> and <see cref="UnionType"/>.
    ///   </item>
    ///   <item>
    ///     Write code generators independent from the specific used <see cref="TypeSchema"/> like
    ///     <see cref="JSON.JsonTypeSchema"/> or <see cref="Native.NativeTypeSchema"/>. 
    ///   </item>
    ///   <item>
    ///     Enable implementation of <see cref="Validation.JsonValidator"/> being independent from a specific
    ///     <see cref="TypeSchema"/> like <see cref="JSON.JsonTypeSchema"/> or <see cref="Native.NativeTypeSchema"/>.
    ///   </item>
    ///   <item>
    ///     Resolving all type references by <see cref="TypeDef"/>'s defined in a type system / schema in advance
    ///     to simplify type access and avoiding type lookups. E.g. references like <see cref="TypeDef.BaseType"/> or
    ///     <see cref="FieldDef.type"/>. 
    ///   </item>
    /// </list>  
    /// <br></br>
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
    /// </summary>
    public abstract class TypeSchema
    {
        /// <summary>Set of all types defined in the type system / schema.</summary>
        public abstract     ICollection<TypeDef>    Types           { get; }
        /// <summary>Set of all well known / standard types used in the type system / schema like integers,
        /// floating point numbers, strings, booleans and timestamps</summary>
        public abstract     StandardTypes           StandardTypes   { get; }
    }
}