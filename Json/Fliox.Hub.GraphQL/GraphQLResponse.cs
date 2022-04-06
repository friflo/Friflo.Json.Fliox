// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public class GqlResponse {
        public  GqlData     data;
    }
    
    public class GqlData {
        [Fri.Property(Name =     "__schema")]
        public  GqlSchema           schema;
    }
    
    public class GqlSchema {
        public  GqlQueryType        queryType;
        public  List<GqlType>       types;
        public  List<GqlDirective>  directives;
    }
    
    public class GqlQueryType {
        public  string      name;
        public  string      description;
    }
    
    [Fri.Discriminator("kind")]
    [Fri.Polymorph(typeof(GqlScalar),       Discriminant = "SCALAR")]
    [Fri.Polymorph(typeof(GqlObject),       Discriminant = "OBJECT")]
    [Fri.Polymorph(typeof(GqlInterface),    Discriminant = "INTERFACE")]
    [Fri.Polymorph(typeof(GqlList),         Discriminant = "LIST")]
    [Fri.Polymorph(typeof(GqlNonNull),      Discriminant = "NON_NULL")]
    [Fri.Polymorph(typeof(GqlInputObject),  Discriminant = "INPUT_OBJECT")]
    [Fri.Polymorph(typeof(GqlUnion),        Discriminant = "UNION")]
    [Fri.Polymorph(typeof(GqlEnum),         Discriminant = "ENUM")]
    public class GqlType {
        public  string      name            { get; set; }
        public  string      description     { get; set; }
    }
    
    public class GqlScalar : GqlType {
    }
    
    public class GqlObject : GqlType {
        public  List<GqlField>  fields;
    }
    
    public class GqlInterface : GqlType {
        public  GqlType         ofType;
    }
    
    public class GqlList : GqlType {
        public  GqlType         ofType;
    }
    
    public class GqlNonNull : GqlType {
        public  GqlType         ofType;
    }
    
    public class GqlField {
        public  string          name;
        public  List<GqlArg>    args;
        public  GqlType         type;
    }
    
    public class GqlUnion : GqlType {
        public  List<GqlType>   possibleTypes;
    }
    
    public class GqlEnum : GqlType {
        public  List<GqlEnumValue>  enumValues;
    }

    public class GqlEnumValue {
        public  string          name;
        public  string          description;
    }

    
    public class GqlInputObject : GqlType {
        public  GqlType         type;
        public  List<GqlField>  inputFields;
    }
    
    public class GqlDirective {
        public  string          name;
        public  string          description;
        public  List<string>    locations;
        public  List<GqlArg>    args;
    }
    
    public class GqlArg {
        public  string          name;
        public  string          description;
        public  GqlType         type;
        public  string          defaultValue;
    }
}