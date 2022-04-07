// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS0649
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class GqlRequest {
        public  string                      query;
        public  string                      operationName;
        public  Dictionary<string,string>   variables;
    }
        
    public class GqlResponse {
        public  GqlData             data;
    }
    
    public class GqlData {
        [Fri.Property(Name =     "__schema")]
        public  JsonValue           schema;
    }
}