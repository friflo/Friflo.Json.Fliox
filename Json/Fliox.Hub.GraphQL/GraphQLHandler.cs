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
using Friflo.Json.Fliox.Schema.GraphQL;
using GraphQLParser;
using GraphQLParser.AST;


#pragma warning disable CS0649
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public class GraphQLHandler: IRequestHandler
    {
        private const   string  GraphQLRoute = "/graphql";
        
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
            var dbName      = context.route.Substring(GraphQLRoute.Length + 1);
            var hub         = context.hub;
            if (!hub.TryGetDatabase(dbName, out _)) {
                context.WriteString($"database not found: {dbName}", "text/html", 400);
                return;
            }
            var method  = context.method;
            if (method == "POST") {
                var body    = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                HandlePost(context, body);
                return;
            }
            if (method == "GET") {
                var html = HtmlGraphiQL.Get(dbName);
                context.WriteString(html, "text/html", 200);
                return;
            }
        }

        private static void HandlePost(RequestContext context, JsonValue body) {
            var pool    = context.Pool;
            GraphQLPost postBody;
            using (var pooled = pool.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                postBody    = reader.Read<GraphQLPost>(body);
            }
            var query       = Parser.Parse(postBody.query);
            switch (postBody.operationName) {
                case "IntrospectionQuery":
                    IntrospectionQuery(context, query);
                    return;
                default:
                    context.WriteError("Invalid operation", postBody.operationName, 400);
                    return;
            }
        }
        
        // ReSharper disable once UnusedParameter.Local
        private static void IntrospectionQuery (RequestContext context, GraphQLDocument query) {
            // var queryString = query.Source.ToString();
            // Console.WriteLine("-------------------------------- query --------------------------------");
            // Console.WriteLine(queryString);
            
            var types = TestAPI.Types;
            
            var schema = new GqlSchema {
                queryType   = new GqlType { name = "Query" },
                types       = types,
                directives  = new List<GqlDirective>()
            };
            var response = new GqlResponse {
                data = new GqlData {
                    schema = schema
                }
            };
            var pool        = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var responseBody        = new JsonValue(writer.WriteAsArray(response));
                File.WriteAllText("Json/Fliox.Hub.GraphQL/temp/graphql-meta.json", responseBody.AsString());
                var testResponse = File.ReadAllText ("Json/Fliox.Hub.GraphQL/temp/response.json", Encoding.UTF8);
                context.Write(responseBody, responseBody.Length, "application/json", 200);
                // context.WriteString(testResponse, "application/json", 200);
                // Console.WriteLine(responseBody.AsString());
            }
        }

        internal class GraphQLPost
        {
            public  string                      query;
            public  string                      operationName;
            public  Dictionary<string,string>   variables;
        }
        
        public class GqlResponse {
            public  GqlData             data;
        }
    
        public class GqlData {
            [Fri.Property(Name =     "__schema")]
            public  GqlSchema           schema;
        }
    }
}

#endif