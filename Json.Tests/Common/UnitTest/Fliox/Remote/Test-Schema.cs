// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static void Schema_errors() {
            ExecuteHttpFile("Schema/errors.http", "Schema/errors.result.http");
        }

        [Test]
        public static void Schema_main_db_index() {
            var request = HttpRequest("GET", "/schema/main_db/index.html");
            AssertRequest(request, 200, "text/html");
        }

        [Test]
        public static void Schema_main_db_json_schema_index() {
            var request = HttpRequest("GET", "/schema/main_db/json-schema/index.html");
            AssertRequest(request, 200, "text/html");
        }
        
        [Test]
        public static void Schema_main_db_json_schema() {
            var request = HttpRequest("GET", "/schema/main_db/json-schema.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static void Schema_main_db_json_schema_directory() {
            var request = HttpRequest("GET", "/schema/main_db/json-schema/directory");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static void Schema_main_db_json_schema_zip() {
            var request = HttpRequest("GET", "/schema/main_db/json-schema/PocStore.json-schema.zip");
            AssertRequest(request, 200, "application/zip");
        }
        
        [Test]
        public static void Schema_main_db_open_api() {
            var request = HttpRequest("GET", "/schema/main_db/open-api.html");
            AssertRequest(request, 200, "text/html");
        }
    }
}
