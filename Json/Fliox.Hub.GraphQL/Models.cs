// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS0649
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal sealed class GqlRequest {
        public  string                          query;
        public  string                          operationName;
        public  Dictionary<string, JsonValue>   variables;
    }
        
    internal sealed class GqlResponse {
        public  Dictionary <string, JsonValue>  data;
        public  List<GqlError>                  errors;
    }
    
    internal sealed class GqlQueryResult {
        public  int             count;
        public  string          cursor;
        public  List<JsonValue> items;
    }
    
    /// <summary>
    ///   <a href="https://spec.graphql.org/June2018/#sec-Errors"> GraphQL > Response > Errors </a>
    /// </summary>
    internal struct GqlError {
        public  string                  message;
        public  List<GqlErrorLocation>  locations;
        public  List<string>            path;
        public  GqlErrorExtensions      extensions;

        public  override string         ToString() => message;
    }
    
    internal struct GqlErrorLocation {
        public  int             line;
        public  int             column;
    }
    
    internal struct GqlErrorExtensions {
        public  TaskErrorType   type;
        public  string          stacktrace;
    }
    
    internal static class ModelUtils
    {
        internal static JsonValue CreateSchemaResponse(ObjectPool<ObjectMapper> mapper, GqlSchema gqlSchema) {
            using (var pooled = mapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var schemaJson          = writer.WriteAsValue(gqlSchema);
                
                var data = new Dictionary<string, JsonValue> {
                    { "__schema", schemaJson }
                };
                var response = new GqlResponse { data = data };
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                return writer.WriteAsValue(response);
            }
        }
    }
}