// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Req = Friflo.Json.Fliox.RequiredMemberAttribute;

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
        [Req]   public  GqlType             queryType;
                public  GqlType             mutationType;
                public  GqlType             subscriptionType;
        [Req]   public  List<GqlType>       types;
                public  List<GqlDirective>  directives;
    }
    
    // ------------------------------------- GraphQL Type ------------------------------------- 
    [Discriminator("kind")]
    [Polymorph(typeof(GqlScalar),       Discriminant = "SCALAR")]
    [Polymorph(typeof(GqlObject),       Discriminant = "OBJECT")]
    [Polymorph(typeof(GqlInterface),    Discriminant = "INTERFACE")]
    [Polymorph(typeof(GqlUnion),        Discriminant = "UNION")]
    [Polymorph(typeof(GqlEnum),         Discriminant = "ENUM")]
    [Polymorph(typeof(GqlInputObject),  Discriminant = "INPUT_OBJECT")]
    [Polymorph(typeof(GqlList),         Discriminant = "LIST")]
    [Polymorph(typeof(GqlNonNull),      Discriminant = "NON_NULL")]
    public class GqlType {
        [Req]   public  string      name        { get; set; }
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
        [Req]   public  string              name;
                public  string              description;
        [Req]   public  List<GqlInputValue> args = new List<GqlInputValue>();
        [Req]   public  GqlType             type;
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
        [Req]   public  string      name;
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
        [Req]   public  string                      name;
                public  string                      description;
        [Req]   public  List<GqlDirectiveLocation>  locations;
        [Req]   public  List<GqlInputValue>         args;
        
        public override string                      ToString() => name;
    }
    
    public sealed class GqlInputValue {
        [Req]   public  string      name;
                public  string      description;
        [Req]   public  GqlType     type;
                public  string      defaultValue;
                
        public override string      ToString() => name;
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