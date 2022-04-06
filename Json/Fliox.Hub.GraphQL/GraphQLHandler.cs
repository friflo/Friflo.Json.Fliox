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
using GraphQLParser.Visitors;

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
            var document    = Parser.Parse(postBody.query);
            var queryString = document.Source.ToString();
            Console.WriteLine("-------------------------------- query --------------------------------");
            Console.WriteLine(queryString);
            context.WriteString("{}", "application/json", 200);
        }
        
        internal class GraphQLPost
        {
            public  string                      query;
            public  string                      operationName;
            public  Dictionary<string,string>   variables;
        }
    }
}