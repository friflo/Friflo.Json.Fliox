// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.IO;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        // ------------------------------------ general errors -----------------------------------
        [Test, Order(0)]
        public static void GraphQL__index() {
            var request = RestRequest("GET", "/graphql/main_db");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test, Order(0)]
        public static void GraphQL__handler_errors() {
            var request = RestRequest("POST", "/graphql/main_db", "", "\"invalid operation\"");
            AssertRequest(request, 400, "text/plain", "invalid request body > JsonReader/error: Cannot assign string to class GqlRequest. got: 'invalid operation' path: '(root)' at position: 19");
        }
        
        [Test, Order(0)]
        public static void GraphQL__error_invalid_path() {
            var query   = "{ __typename }";
            var request = GraphQLRequest("/graphql", query);
            AssertRequest(request, 400, "text/plain", "invalid path > expect: graphql/database");
        }
        
        [Test, Order(0)]
        public static void GraphQL__error_database_not_found() {
            var query   = "{ __typename }";
            var request = GraphQLRequest("/graphql/unknown_db", query);
            AssertRequest(request, 404, "text/plain", "error: database not found, database: unknown_db");
        }
        
        [Test, Order(0)]
        public static void GraphQL__error_invalid_query() {
            var query   = "{";
            var request = GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "invalid_query.txt");
            AssertRequest(request, 400, "text/plain");
        }
        
        // --------------------------------------- main_db ---------------------------------------
        /// <summary>
        /// use name <see cref="GraphQL__main_db_init"/> to run as first test.
        /// This forces loading all required <see cref="Friflo.Json.Fliox.Hub.GraphQL.GraphQLHandler"/> code
        /// and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static void GraphQL__main_db_init() {
            var query       = ReadGraphQLQuery ("main_db/queries.graphql");
            GraphQLRequest("/graphql/main_db", query);
        }
        
        [Test, Order(1)]
        public static void GraphQL_main_db_IntrospectionQuery() {
            var query   = ReadGraphQLQuery ("main_db/introspection.graphql");
            var request = GraphQLRequest("/graphql/main_db", query, "IntrospectionQuery");
            WriteGraphQLResponse(request, "main_db/introspection.json");
        }
        
        [Test, Order(1)]
        public static void GraphQL_main_db_queries() {
            var query   = ReadGraphQLQuery ("main_db/queries.graphql");
            var request = GraphQLRequest("/graphql/main_db", query);
            // for (int n = 0; n < 10_000; n++) { GraphQLRequest("/graphql/main_db", query); }
            WriteGraphQLResponse(request, "main_db/queries.json");
        }
        
        [Test, Order(1)]
        public static void GraphQL_main_db_errors() {
            var query   = ReadGraphQLQuery ("main_db/errors.graphql");
            var request = GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "main_db/errors.json");
        }
        
        [Test, Order(1)]
        public static void GraphQL_main_db_std_Echo() {
            var query   = ReadGraphQLQuery ("main_db/std_Echo.graphql");
            var request = GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "main_db/std_Echo.json");
        }
        
        [Test, Order(2)]
        public static void GraphQL_main_db_mutationErrors() {
            var query   = ReadGraphQLQuery ("main_db/mutationErrors.graphql");
            var request = GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "main_db/mutationErrors.json");
        }
        
        [Test, Order(2)]
        public static void GraphQL_main_db_mutations() {
            var query   = ReadGraphQLQuery ("main_db/mutations.graphql");
            var request = GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "main_db/mutations.json");
        }
        
        // --------------------------------------- user_db ---------------------------------------
        /// <summary>
        /// use name <see cref="GraphQL__user_db_init"/> to run as first test.
        /// This forces loading all required <see cref="Friflo.Json.Fliox.Hub.GraphQL.GraphQLHandler"/> code
        /// and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static void GraphQL__user_db_init() {
            var query   = ReadGraphQLQuery ("user_db/std_Echo.graphql");
            var request = GraphQLRequest("/graphql/user_db", query);
            WriteGraphQLResponse(request, "user_db/std_Echo.json");
        }
        
        [Test, Order(1)]
        public static void GraphQL_user_db_queries() {
            var query   = ReadGraphQLQuery ("user_db/queries.graphql");
            var request = GraphQLRequest("/graphql/user_db", query);
            WriteGraphQLResponse(request, "user_db/queries.json");
        }
        
        // --------------------------------------- utils ---------------------------------------
        private static readonly string GraphQLAssets = CommonUtils.GetBasePath() + "assets~/Remote/GraphQL/";

        private static string ReadGraphQLQuery(string path) {
            return File.ReadAllText(GraphQLAssets + path);
        }
        
        private static void WriteGraphQLResponse(RequestContext request, string path) {
            File.WriteAllBytes(GraphQLAssets + path, request.Response.AsByteArray());
        }
    }
}

#endif
