// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Schema.GraphQL
{
    /// <summary>
    /// <a href ="https://spec.graphql.org/June2018/#sec-Schema-Introspection">GraphQL specification - Schema Introspection</a>
    /// </summary>
    public sealed class GqlSchema {
        [Required]  public  GqlType             queryType;
                    public  GqlType             mutationType;
                    public  GqlType             subscriptionType;
        [Required]  public  List<GqlType>       types;
                    public  List<GqlDirective>  directives;
    }
    
    // ------------------------------------- GraphQL Type ------------------------------------- 
    [Discriminator("kind")]
    [PolymorphType(typeof(GqlScalar),       Discriminant = "SCALAR")]
    [PolymorphType(typeof(GqlObject),       Discriminant = "OBJECT")]
    [PolymorphType(typeof(GqlInterface),    Discriminant = "INTERFACE")]
    [PolymorphType(typeof(GqlUnion),        Discriminant = "UNION")]
    [PolymorphType(typeof(GqlEnum),         Discriminant = "ENUM")]
    [PolymorphType(typeof(GqlInputObject),  Discriminant = "INPUT_OBJECT")]
    [PolymorphType(typeof(GqlList),         Discriminant = "LIST")]
    [PolymorphType(typeof(GqlNonNull),      Discriminant = "NON_NULL")]
    public class GqlType {
        [Required]  public  string      name        { get; set; }
                    public  string      description { get; set; }
                
        public override string      ToString()  => name;
    }
    
    public sealed class GqlScalar      : GqlType {
    }
    
    public sealed class GqlObject      : GqlType {
                public  List<GqlField>      fields;
                public  List<GqlType>       interfaces = new List<GqlType>();
    }
    
    public sealed class GqlField {
        [Required]  public  string              name;
                    public  string              description;
        [Required]  public  List<GqlInputValue> args = new List<GqlInputValue>();
        [Required]  public  GqlType             type;
                    public  bool?               isDeprecated;
                    public  string              deprecationReason;

        public override string              ToString() => name;
    }
    
    public sealed class GqlInterface   : GqlType {
                public  GqlType             ofType;
    }
    
    public sealed class GqlUnion       : GqlType {
                public  List<GqlType>       possibleTypes;
    }
    
    public sealed class GqlEnum        : GqlType {
                    public  List<GqlEnumValue>  enumValues;
    }
    
    public sealed class GqlEnumValue {
        [Required]  public  string      name;
                    public  string      description;
                    public  bool?       isDeprecated;
                    public  string      deprecationReason;
                
        public override string      ToString() => name;
    }
    
    public sealed class GqlInputObject : GqlType {
                public  GqlType             type;
                public  List<GqlField>      inputFields;
    }
    
    public sealed class GqlList        : GqlType {
                public  GqlType     ofType;
    }
    
    public sealed class GqlNonNull     : GqlType {
                    public  GqlType     ofType;
    }

    public sealed class GqlDirective {
        [Required]  public  string                      name;
                    public  string                      description;
        [Required]  public  List<GqlDirectiveLocation>  locations;
        [Required]  public  List<GqlInputValue>         args;
        
        public override     string                      ToString() => name;
    }
    
    public sealed class GqlInputValue {
        [Required]  public  string      name;
                    public  string      description;
        [Required]  public  GqlType     type;
                    public  string      defaultValue;
                
        public override     string      ToString() => name;
    }
    
    public enum GqlDirectiveLocation {
        QUERY,
        MUTATION,
        SUBSCRIPTION,
        FIELD,
        FRAGMENT_DEFINITION,
        FRAGMENT_SPREAD,
        INLINE_FRAGMENT,
        SCHEMA,
        SCALAR,
        OBJECT,
        FIELD_DEFINITION,
        ARGUMENT_DEFINITION,
        INTERFACE,
        UNION,
        ENUM,
        ENUM_VALUE,
        INPUT_OBJECT,
        INPUT_FIELD_DEFINITION,
    }
}