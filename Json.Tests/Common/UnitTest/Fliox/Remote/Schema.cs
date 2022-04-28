// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static async Task Schema_main_db_index() {
            var request = await RestRequest("GET", "/schema/main_db/index.html");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test]
        public static async Task Schema_main_db_json_schema_index() {
            var request = await RestRequest("GET", "/schema/main_db/json-schema/index.html");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test]
        public static async Task Schema_main_db_json_schema() {
            var request = await RestRequest("GET", "/schema/main_db/json-schema.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static async Task Schema_main_db_json_schema_directory() {
            var request = await RestRequest("GET", "/schema/main_db/json-schema/directory");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static async Task Schema_main_db_json_schema_zip() {
            var request = await RestRequest("GET", "/schema/main_db/json-schema/PocStore.json-schema.zip");
            AssertRequest(request, 200, "application/zip");
        }
        
        [Test]
        public static async Task Schema_main_db_open_api() {
            var request = await RestRequest("GET", "/schema/main_db/open-api.html");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test]
        public static async Task Schema_main_db_typescript_schema() {
            var request = await RestRequest("GET", "/schema/main_db/typescript/unknown");
            AssertRequest(request, 404, "text/plain", "schema error > file not found: 'unknown'");
        }
    }
}

#endif