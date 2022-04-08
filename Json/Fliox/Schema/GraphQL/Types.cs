// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

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
    public class GqlSchema {
        [Req]   public  GqlType             queryType;
                public  GqlType             mutationType;
                public  GqlType             subscriptionType;
        [Req]   public  List<GqlType>       types;
                public  List<GqlDirective>  directives;
    }
    
    // ------------------------------------- GraphQL Type ------------------------------------- 
    [Fri.Discriminator("kind")]
    [Fri.Polymorph(typeof(GqlScalar),       Discriminant = "SCALAR")]
    [Fri.Polymorph(typeof(GqlObject),       Discriminant = "OBJECT")]
    [Fri.Polymorph(typeof(GqlInterface),    Discriminant = "INTERFACE")]
    [Fri.Polymorph(typeof(GqlUnion),        Discriminant = "UNION")]
    [Fri.Polymorph(typeof(GqlEnum),         Discriminant = "ENUM")]
    [Fri.Polymorph(typeof(GqlInputObject),  Discriminant = "INPUT_OBJECT")]
    [Fri.Polymorph(typeof(GqlList),         Discriminant = "LIST")]
    [Fri.Polymorph(typeof(GqlNonNull),      Discriminant = "NON_NULL")]
    public class GqlType {
        [Req]   public  string      name        { get; set; }
                public  string      description { get; set; }
                
        public override string      ToString()  => name;
    }
    
    public class GqlScalar      : GqlType {
    }
    
    public class GqlObject      : GqlType {
                public  List<GqlField>      fields;
                public  List<GqlType>       interfaces = new List<GqlType>();
    }
    
    public class GqlField {
        [Req]   public  string              name;
                public  string              description;
        [Req]   public  List<GqlInputValue> args = new List<GqlInputValue>();
        [Req]   public  GqlType             type;
                public  bool?               isDeprecated;
                public  string              deprecationReason;

        public override string              ToString() => name;
    }
    
    public class GqlInterface   : GqlType {
                public  GqlType             ofType;
    }
    
    public class GqlUnion       : GqlType {
                public  List<GqlType>       possibleTypes;
    }
    
    public class GqlEnum        : GqlType {
                public  List<GqlEnumValue>  enumValues;
    }
    
    public class GqlEnumValue {
        [Req]   public  string      name;
                public  string      description;
                public  bool?       isDeprecated;
                public  string      deprecationReason;
                
        public override string      ToString() => name;
    }
    
    public class GqlInputObject : GqlType {
                public  GqlType             type;
                public  List<GqlField>      inputFields;
    }
    
    public class GqlList        : GqlType {
                public  GqlType     ofType;
    }
    
    public class GqlNonNull     : GqlType {
                public  GqlType     ofType;
    }

    public class GqlDirective {
        [Req]   public  string                      name;
                public  string                      description;
        [Req]   public  List<GqlDirectiveLocation>  locations;
        [Req]   public  List<GqlInputValue>         args;
        
        public override string                      ToString() => name;
    }
    
    public class GqlInputValue {
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