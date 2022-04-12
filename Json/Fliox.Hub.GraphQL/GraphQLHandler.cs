// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Utils;
using GraphQLParser;
using GraphQLParser.AST;

// ReSharper disable ConvertIfStatementToSwitchStatement
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public class GraphQLHandler: IRequestHandler
    {
        private readonly    Dictionary<string, QLDatabaseSchema>    dbSchemas       = new Dictionary<string, QLDatabaseSchema>();
        private const       string                                  GraphQLRoute    = "/graphql";
        
        public string[]     Routes => new [] { GraphQLRoute };
        
        public bool IsMatch(RequestContext context) {
            var method = context.method;
            if (method != "POST" && method != "GET")
                return false;
            return RequestContext.IsBasePath(GraphQLRoute, context.route);
        }
        
        public async Task HandleRequest(RequestContext context) {
            if (context.route == GraphQLRoute) {
                context.WriteError("invalid path", "expect: graphql/database", 400);
                return;
            }
            var dbName  = context.route.Substring(GraphQLRoute.Length + 1);
            var schema  = GetSchema(context, dbName, out string error);
            if (schema == null) {
                context.WriteString($"error: {error}, database: {dbName}", "text/html", 400);
                return;
            }
            var method  = context.method;
            // ------------------    GET            /graphql/{database}
            if (method == "GET") {
                var html = HtmlGraphiQL.Get(dbName, schema.schemaName);
                context.WriteString(html, "text/html", 200);
                return;
            }
            if (method == "POST") {
                var mapper          = context.ObjectMapper;
                var body            = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                var postBody        = ReadRequestBody(mapper, body);
                var docStr          = postBody.query;
                var query           = Parser.Parse(docStr);
                var operationName   = postBody.operationName;
                
                // --------------    POST           /graphql/{database}     case: "operationName" == "IntrospectionQuery"
                if (operationName == "IntrospectionQuery") {
                    var schemaResponse = IntrospectionQuery(mapper, query, schema.schemaResponse);
                    context.Write(schemaResponse, schemaResponse.Length, "application/json", 200);
                    return;
                }
                // --------------    POST           /graphql/{database}     case: any other "operationName"
                var request = schema.requestHandler.CreateRequest(operationName, query, docStr, out error);
                if (error != null) {
                    context.WriteError("invalid request", error, 400);
                    return;
                }
                var syncRequest     = request.syncRequest; 
                var cookies         = context.cookies;
                syncRequest.userId  = new JsonKey(cookies["fliox-user"]); 
                syncRequest.token   = cookies["fliox-token"];
                var executeContext  = context.CreateExecuteContext(null);
                var syncResult      = await context.hub.ExecuteSync(syncRequest, executeContext).ConfigureAwait(false);

                if (syncResult.error != null) {
                    context.WriteError("execution error", syncResult.error.message, 500);
                    return;
                }
                var opResponse      = QLResponseHandler.ProcessResponse(mapper, request.queries, syncResult.success);
                context.Write(opResponse, 0, "application/json", 200);
                return;
            }
            context.WriteError("invalid request", context.ToString(), 400);
        }
        
        private static GqlRequest ReadRequestBody(ObjectPool<ObjectMapper> mapper, JsonValue body) {
            using (var pooled = mapper.Get()) {
                var reader  = pooled.instance.reader;
                return reader.Read<GqlRequest>(body);
            }
        }
        
        private QLDatabaseSchema GetSchema (RequestContext context, string dbName, out string error) {
            var hub         = context.hub;
            if (!hub.TryGetDatabase(dbName, out var database)) {
                error = $"database not found: {dbName}";
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

        private static JsonValue IntrospectionQuery (ObjectPool<ObjectMapper> mapper, GraphQLDocument query, JsonValue schemaResponse) {
            // schemaResponse = TestAPI.CreateTestSchema(mapper);
            
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