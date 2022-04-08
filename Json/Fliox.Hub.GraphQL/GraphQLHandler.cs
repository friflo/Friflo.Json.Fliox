// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.GraphQL.Lab;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Language;
using GraphQLParser;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class GraphQLSchema {
        private     readonly    string      database;
        internal    readonly    JsonValue   schemaResponse;

        public      override    string      ToString() => database;

        internal GraphQLSchema(string database, JsonValue schemaResponse) {
            this.database       = database;
            this.schemaResponse = schemaResponse;
        } 
    }
    
    public class GraphQLHandler: IRequestHandler
    {
        private readonly    Dictionary<string, GraphQLSchema>   schemas         = new Dictionary<string, GraphQLSchema>();
        private const       string                              GraphQLRoute    = "/graphql";
        
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
            // --------------    POST           /graphql/{database}
            if (method == "POST") {
                var body    = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                var postBody    = ReadRequestBody(context, body);
                var query       = Parser.Parse(postBody.query);
                if (postBody.operationName == "IntrospectionQuery") {
                    IntrospectionQuery(context, query, schema.schemaResponse);
                    return;
                }
                await GraphQLQuery.Execute(context, schema);
                return;
            }
            // --------------    GET            /graphql/{database}
            if (method == "GET") {
                var html = HtmlGraphiQL.Get(dbName);
                context.WriteString(html, "text/html", 200);
                return;
            }
        }
        
        private static GqlRequest ReadRequestBody(RequestContext context, JsonValue body) {
            var pool    = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                return reader.Read<GqlRequest>(body);
            }
        }
        
        private GraphQLSchema GetSchema (RequestContext context, string databaseName, out string error) {
            var hub         = context.hub;
            if (!hub.TryGetDatabase(databaseName, out var database)) {
                error = $"database not found: {databaseName}";
                return default;
            }
            if (schemas.TryGetValue(databaseName, out var schema)) {
                error = null;
                return schema;
            }
            error                   = null;
            var typeSchema          = database.Schema.typeSchema;
            var generator           = new Generator(typeSchema, ".json");
            var gqlSchema           = GraphQLGenerator.Generate(generator);
            
            var schemaResponse      = Utils.CreateSchemaResponse(context.Pool, gqlSchema);
            schema                  = new GraphQLSchema (databaseName, schemaResponse);
            schemas[databaseName]   = schema;
            return schema;
        }

        private static void IntrospectionQuery (RequestContext context, GraphQLDocument query, JsonValue schemaResponse) {
            // schemaResponse = TestAPI.CreateTestSchema(context.Pool);
            
            // var queryString = query.Source.ToString();
            // Console.WriteLine("-------------------------------- query --------------------------------");
            // Console.WriteLine(queryString);

            File.WriteAllText("Json/Fliox.Hub.GraphQL/temp/schema.json", schemaResponse.AsString());
            var testResponse = File.ReadAllText ("Json/Fliox.Hub.GraphQL/temp/response.json", Encoding.UTF8);
            context.Write(schemaResponse, schemaResponse.Length, "application/json", 200);
            // context.WriteString(testResponse, "application/json", 200);
            // Console.WriteLine(responseBody.AsString());
        }
    }
}

#endif