// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.IO;
using System.Threading.Tasks;
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
        public static async Task Rest__main_db_init() {
            var request = await RestRequest("POST", "/rest/main_db/", "?command=std.Echo");
            AreEqual ("null", request.Response.AsString());
        }
        
        [Test, Order(1)]
        public static async Task Rest_main_db_std_Echo() {
            var request = await RestRequest("POST", "/rest/main_db/", "?command=std.Echo", "{}");
            AreEqual ("{}", request.Response.AsString());
        }
        
        [Test, Order(1)]
        public static async Task Rest_main_db_GET_Entity() {
            var request = await RestRequest("GET", "/rest/main_db/articles/article-1");
            WriteRestResponse(request, "main_db/GET.article.json");
        }
        
        [Test, Order(1)]
        public static async Task Rest_main_db_GET_Entities() {
            var request = await RestRequest("GET", "/rest/main_db/articles");
            WriteRestResponse(request, "main_db/GET.articles.json");
        }
        
        // [Test, Order(1)]
        public static async Task Rest_main_db_POST_bulk_get() {
            var request = await RestRequest("POST", "/rest/main_db/articles");
            WriteRestResponse(request, "main_db/POST.bulk-get.articles.json");
        }
        
        [Test, Order(2)]
        public static async Task Rest_main_db_PUT_Entities() {
            var body = ReadRestRequest ("main_db/PUT.articles.json");
            var request = await RestRequest("PUT", "/rest/main_db/articles", "", body);
            AssertRequest(request, 200, "text/plain", "PUT successful");
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

#endif