// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        /// <summary>
        /// use name <see cref="GraphQL__Init"/> to run as first test.
        /// This forces loading all required <see cref="Friflo.Json.Fliox.Hub.GraphQL.GraphQLHandler"/> code
        /// and subsequent tests show the real execution time
        /// </summary>
        [Test]
        public static async Task GraphQL__Init() {
            var query       = ReadGraphQLQuery ("queries.graphql");
            await GraphQLRequest("/graphql/main_db", query);
        }
        
        [Test]
        public static async Task GraphQL_IntrospectionQuery() {
            var query       = ReadGraphQLQuery ("introspection.graphql");
            var request     = await GraphQLRequest("/graphql/main_db", query, "IntrospectionQuery");
            WriteGraphQLResponse(request, "introspection.json");
        }
        
        [Test]
        public static async Task GraphQL_queries() {
            var query       = ReadGraphQLQuery ("queries.graphql");
            var request     = await GraphQLRequest("/graphql/main_db", query);
            // for (int n = 0; n < 10_000; n++) { await GraphQLRequest("/graphql/main_db", query); }
            WriteGraphQLResponse(request, "queries.json");
        }

        [Test]
        public static async Task GraphQL_std_Echo() {
            var query   = "{ std_Echo (param: 123) }";
            var request = await GraphQLRequest("/graphql/main_db", query);
            WriteGraphQLResponse(request, "std_Echo.json");
        }
        
        // --------------------------------------- utils ---------------------------------------
        private static readonly string GraphQLAssets = CommonUtils.GetBasePath() + "assets~/GraphQL/";

        private static string ReadGraphQLQuery(string path) {
            return File.ReadAllText(GraphQLAssets + path);
        }
        
        private static void WriteGraphQLResponse(RequestContext request, string path) {
            File.WriteAllBytes(GraphQLAssets + path, request.Response.AsByteArray());
        }
    }
}

#endif
