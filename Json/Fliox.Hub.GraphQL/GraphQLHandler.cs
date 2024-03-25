// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Transform.Project;
using Friflo.Json.Fliox.Utils;
using GraphQLParser;
using GraphQLParser.AST;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable ConvertIfStatementToSwitchStatement
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    /// <summary>
    /// An <see cref="GraphQLHandler"/> provide access to a <see cref="HttpHost"/> using <b>GraphQL</b>
    /// </summary>
    /// <remarks>
    /// For each database a GraphQL schema is generated based on the <see cref="DatabaseSchema"/> assigned to each
    /// <see cref="EntityDatabase"/>.  
    /// </remarks>
    public sealed class GraphQLHandler : IRequestHandler
    {
        private readonly    Dictionary<string, QLDatabaseSchema>    dbSchemas;
        private readonly    ObjectPool<JsonProjector>               projectorPool;                
        private const       string                                  GraphQLRoute    = "/graphql";
        
        public override     string                                  ToString() => GraphQLRoute;
        
        public string[]     Routes => new [] { GraphQLRoute };
        
        public GraphQLHandler() {
            dbSchemas       = new Dictionary<string, QLDatabaseSchema>();
            projectorPool   = new ObjectPool<JsonProjector>(() => new JsonProjector());
        }
        
        public bool IsMatch(RequestContext context) {
            var method = context.method;
            if (method != "POST" && method != "GET")
                return false;
            return RequestContext.IsBasePath(GraphQLRoute, context.route);
        }
        
        public async Task<bool> HandleRequest(RequestContext context) {
            if (context.route == GraphQLRoute) {
                context.WriteError("invalid path", "expect: graphql/database", 400);
                return true;
            }
            var dbName  = context.route.Substring(GraphQLRoute.Length + 1);
            var schema  = GetSchema(context, dbName, out string error);
            if (schema == null) {
                context.WriteString($"error: {error}, database: {dbName}", "text/plain", 404);
                return true;
            }
            var method  = context.method;
            // ------------------    GET            /graphql/{database}
            if (method == "GET") {
                var html = HtmlGraphiQL.Get(dbName, schema.schemaName);
                context.WriteString(html, "text/html", 200);
                return true;
            }
            if (method == "POST") {
                using (var pooled = context.ObjectMapper.Get()) {
                    var mapper          = pooled.instance;
                    var body            = await JsonValue.ReadToEndAsync(context.body, context.contentLength).ConfigureAwait(false);
                    var gqlRequest      = ReadRequestBody(mapper, body, out error);
                    if (error != null) {
                        context.WriteError("invalid request body", error, 400);
                        return true;
                    }
                    var doc             = gqlRequest.query;
                    var query           = ParseGraphQL(doc, out error);
                    if (error != null) {
                        context.WriteError("invalid GraphQL query", error, 400);
                        return true;
                    }
                    var operationName   = gqlRequest.operationName;
                    
                    // --------------    POST           /graphql/{database}     case: "operationName" == "IntrospectionQuery"
                    if (operationName == "IntrospectionQuery") {
                        var schemaResponse = IntrospectionQuery(mapper, query, schema.schemaResponse);
                        context.Write(schemaResponse, "application/json", 200);
                        return true;
                    }
                    // --------------    POST           /graphql/{database}     case: any other "operationName"
                    var request         = schema.requestHandler.CreateRequest(mapper, gqlRequest, query, doc);
                    var syncRequest     = request.syncRequest; 
                    var headers         = context.headers;
                    syncRequest.userId  = new ShortString(headers.Cookie("fliox-user")); 
                    syncRequest.token   = new ShortString(headers.Cookie("fliox-token"));
                    var syncContext     = new SyncContext(context.hub.sharedEnv, null, context.memoryBuffer) { Host = context.host }; // new context per request
                    
                    var hub             = context.hub;
                    var executionType   = hub.InitSyncRequest(syncRequest);
                    ExecuteSyncResult syncResult;
                    switch (executionType) {
                        case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                        case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                        default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;    
                    }
                    if (syncResult.error != null) {
                        context.WriteError("execution error", syncResult.error.message, 500);
                        return true;
                    }
                    var opResponse = QLResponseHandler.Process(mapper, projectorPool, request.queries, syncResult.success);
                    context.Write(opResponse, "application/json", 200);
                    return true;
                }
            }
            context.WriteError("invalid request", context.ToString(), 400);
            return true;
        }
        
        private static GqlRequest ReadRequestBody(ObjectMapper mapper, in JsonValue body, out string error) {
            var reader  = mapper.reader;
            var result = reader.Read<GqlRequest>(body);
            if (reader.Error.ErrSet) {
                error = reader.Error.msg.ToString();
                return null;
            }
            error = null;
            return result;
        }
        
        private static GraphQLDocument ParseGraphQL(string docStr, out string error) {
            try {
                error = null;
                return Parser.Parse(docStr);    
            }
            catch(Exception ex) {
                error = ex.Message;
            }
            return null;
        }
        
        private QLDatabaseSchema GetSchema (RequestContext context, string dbName, out string error) {
            var hub         = context.hub;
            if (!hub.TryGetDatabase(dbName, out var database)) {
                error = $"database not found";
                return default;
            }
            if (dbSchemas.TryGetValue(dbName, out var schema)) {
                error = null;
                return schema;
            }
            error               = null;
            var typeSchema      = database.Schema.typeSchema;
            var generator       = new Generator(typeSchema, ".graphql");
            var gqlSchema       = GraphQLGenerator.Generate(generator);
            var schemaName      = generator.rootType.Name;

            var schemaResponse  = ModelUtils.CreateSchemaResponse(context.ObjectMapper, gqlSchema);
            var queryHandler    = new QLRequestHandler (typeSchema, dbName);
            schema              = new QLDatabaseSchema (dbName, schemaName, gqlSchema, schemaResponse, queryHandler);
            dbSchemas[dbName]   = schema;
            return schema;
        }

        private static JsonValue IntrospectionQuery (ObjectMapper mapper, GraphQLDocument query, JsonValue schemaResponse) {
            // var queryString = query.Source.ToString();
            // Console.WriteLine("-------------------------------- query --------------------------------");
            // Console.WriteLine(queryString);

            // File.WriteAllText("Json/Fliox.Hub.GraphQL/temp/schema.json", schemaResponse.AsString());
            // var schemaResponse = File.ReadAllText ("Json/Fliox.Hub.GraphQL/temp/response.json", Encoding.UTF8);
            return schemaResponse;
        }
    }
}

#endif