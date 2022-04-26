// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static async Task TestGQL_queries() {
            var query       = File.ReadAllText(TestFolder + "queries.graphql");

            var request     = await GraphQLRequest("/graphql/main_db", query);
            
            File.WriteAllBytes(TestFolder + "queries.json", request.Response.AsByteArray());
        }
        
        [Test]
        public static async Task TestGQL_std_Echo() {
            var query   = "{ std_Echo (param: 123) }";
            var request = await GraphQLRequest("/graphql/main_db", query);
            
            File.WriteAllBytes(TestFolder + "std_Echo.json", request.Response.AsByteArray());
        }
    }
}