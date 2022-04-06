// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
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
            if (context.method != "POST")
                return false;
            return context.route == GraphQLRoute;
        }

        public async Task HandleRequest(RequestContext context) {
            var body    = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
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
            
            var types = GraphQLMeta.Types;
            
            var schema = new GqlSchema {
                queryType   = new GqlQueryType { name = "Query" },
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
                context.Write(responseBody, responseBody.Length, "application/json", 200);
                File.WriteAllText("Json/Fliox.Hub.GraphQL/temp/graphql-meta.json", responseBody.AsString());
                // Console.WriteLine(responseBody.AsString());
            }
        }

        internal class GraphQLPost
        {
            public  string                      query;
            public  string                      operationName;
            public  Dictionary<string,string>   variables;
        }
    }
}