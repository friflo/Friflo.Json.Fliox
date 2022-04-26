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
        // [Test]
        public static async Task TestQueries() {
            var query       = File.ReadAllText(TestFolder + "query.graphql");

            var request     = await GraphQLRequest("/graphql/main_db", query);
            
            File.WriteAllBytes(TestFolder + "query.json", request.Response.AsByteArray());
        }
    }
}