// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        /// <summary>
        /// use name <see cref="Rest__main_db_init"/> to run as first test.
        /// This forces loading all required RestHandler code and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static void Rest__main_db_init() {
            var request = RestRequest("POST", "/rest/main_db/", "?command=std.Echo");
            AreEqual ("null", request.Response.AsString());
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_happy() {
            ExecuteRestFile("Rest/main_db/happy-read.http", "Rest/main_db/happy-read.result.http");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_std_Echo() {
            var request = RestRequest("POST", "/rest/main_db/", "?command=std.Echo", "{}");
            AreEqual ("{}", request.Response.AsString());
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_message() {
            var request = RestRequest("POST", "/rest/main_db/", "?message=Message1", "\"foo\"");
            AreEqual ("\"received\"", request.Response.AsString());
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_root() {
            var request = RestRequest("GET", "/rest");
            WriteRestResponse(request, "main_db/GET.root.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_database() {
            var request = RestRequest("GET", "/rest/main_db");
            WriteRestResponse(request, "main_db/GET.database.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_Entities() {
            var request = RestRequest("GET", "/rest/main_db/articles");
            WriteRestResponse(request, "main_db/GET.articles.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_Entities_filter() {
            var request = RestRequest("GET", "/rest/main_db/articles", "filter=o.producer=='producer-samsung'");
            WriteRestResponse(request, "main_db/GET.articles-filter.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_Entities_by_id() {
            var request = RestRequest("GET", "/rest/main_db/articles", "ids=article-1,article-2");
            WriteRestResponse(request, "main_db/GET.articles-by-id.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_GET_Entity() {
            var request = RestRequest("GET", "/rest/main_db/articles/article-1");
            WriteRestResponse(request, "main_db/GET.article.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_POST_bulk_get() {
            var body    = "[\"article-1\",\"article-2\"]";
            var request = RestRequest("POST", "/rest/main_db/articles/bulk-get", "", body);
            WriteRestResponse(request, "main_db/POST.bulk-get.articles.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_PUT_Entity() {
            var body    = ReadRestRequest ("main_db/PUT.article.json");
            var request = RestRequest("PUT", "/rest/main_db/articles/article-PUT-single", "", body);
            AssertRequest(request, 200, "text/plain", "PUT successful");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_PUT_Entities() {
            var body    = ReadRestRequest ("main_db/PUT.articles.json");
            var request = RestRequest("PUT", "/rest/main_db/articles", "", body);
            AssertRequest(request, 200, "text/plain", "PUT successful");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_PATCH_article() {
            var body    = ReadRestRequest ("main_db/PATCH.article.json");
            var request = RestRequest("PATCH", "/rest/main_db/articles/article-ipad", "", body);
            AssertRequest(request, 200, "text/plain", "PATCH successful");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_DELETE_article() {
            var request = RestRequest("DELETE", "/rest/main_db/articles/article-1");
            AssertRequest(request, 200, "text/plain", "deleted successful");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_POST_bulk_delete() {
            var body    = "[\"article-2\"]";
            var request = RestRequest("POST", "/rest/main_db/articles/bulk-delete", "", body);
            AssertRequest(request, 200, "text/plain", "deleted successful");
        }
        
        
        // --------------------------------------- utils ---------------------------------------
        private static readonly string RestAssets = CommonUtils.GetBasePath() + "assets~/Rest/";
        
        private static string ReadRestRequest(string path) {
            return File.ReadAllText(RestAssets + path);
        }

        private static void WriteRestResponse(RequestContext request, string path) {
            File.WriteAllBytes(RestAssets + path, request.Response.AsByteArray());
        }
    }
}
