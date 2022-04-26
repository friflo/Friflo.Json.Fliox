// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        private static readonly string GraphQLAssets = CommonUtils.GetBasePath() + "assets~/GraphQL/";
        
        /// <summary>
        /// use name <see cref="GraphQL__Init"/> to run as first test.
        /// This forces loading all required <see cref="Friflo.Json.Fliox.Hub.GraphQL.GraphQLHandler"/> code
        /// and subsequent tests show the real execution time
        /// </summary>
        [Test]
        public static async Task GraphQL__Init() {
            var query       = File.ReadAllText(GraphQLAssets + "queries.graphql");
            await GraphQLRequest("/graphql/main_db", query);
        }
        
        [Test]
        public static async Task GraphQL_IntrospectionQuery() {
            var query       = File.ReadAllText(GraphQLAssets + "introspection.graphql");
            var request     = await GraphQLRequest("/graphql/main_db", query, "IntrospectionQuery");
            File.WriteAllBytes(GraphQLAssets + "introspection.json", request.Response.AsByteArray());
        }
        
        [Test]
        public static async Task GraphQL_queries() {
            var query       = File.ReadAllText(GraphQLAssets + "queries.graphql");
            var request     = await GraphQLRequest("/graphql/main_db", query);
            // for (int n = 0; n < 10_000; n++) { await GraphQLRequest("/graphql/main_db", query); }
            File.WriteAllBytes(GraphQLAssets + "queries.json", request.Response.AsByteArray());
        }

        [Test]
        public static async Task GraphQL_std_Echo() {
            var query   = "{ std_Echo (param: 123) }";
            var request = await GraphQLRequest("/graphql/main_db", query);
            
            File.WriteAllBytes(GraphQLAssets + "std_Echo.json", request.Response.AsByteArray());
        }
    }
}