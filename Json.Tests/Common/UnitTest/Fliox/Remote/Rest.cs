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
        /// use name <see cref="Rest__Init"/> to run as first test.
        /// This forces loading all required RestHandler code and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static async Task Rest__Init() {
            var request = await RestRequest("POST", "/rest/main_db/", "?command=std.Echo");
            AreEqual ("null", request.Response.AsString());
        }
        
        [Test, Order(1)]
        public static async Task Rest_std_Echo() {
            var request = await RestRequest("POST", "/rest/main_db/", "?command=std.Echo", "{}");
            AreEqual ("{}", request.Response.AsString());
        }
        
        [Test, Order(1)]
        public static async Task Rest_GetEntity() {
            var request = await RestRequest("GET", "/rest/main_db/articles");
            WriteRestResponse(request, "GET-main_db.articles.json");
        }
        
        [Test, Order(2)]
        public static async Task Rest_PutEntities() {
            var body = ReadRestRequest ("PUT-main_db.articles.json");
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