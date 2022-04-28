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
        [Test]
        public static async Task Static_index() {
            var request     = await RestRequest("GET", "/");
            AssertRequest(request, 200, "text/html; charset=UTF-8");
            
            var requestIndex = await RestRequest("GET", "/index.html");
            AssertRequest(requestIndex, 200, "text/html; charset=UTF-8");
            
            var requestCached = await RestRequest("GET", "/index.html");
            AssertRequest(requestCached, 200, "text/html; charset=UTF-8");
            
            var root        = request.Response.AsString();
            var indexHtml   = requestIndex.Response.AsString();
            var cachedHtml  = requestIndex.Response.AsString();
            
            AreEqual(root, indexHtml);
            AreEqual(root, cachedHtml);
        }
        
        [Test]
        public static async Task Static_explorer() {
            var request = await RestRequest("GET", "/explorer");
            AssertRequest(request, 200, "application/json", "[\n]");
        }
        
        [Test]
        public static async Task Static_explorer_example_requests() {
            var request = await RestRequest("GET", "/explorer/example-requests");
            WriteStaticResponse(request, "example-requests.json");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static async Task Static_swagger() {
            var request = await RestRequest("GET", "/swagger");
            AssertRequest(request, 200, "application/json");
        }
        
        [Test]
        public static async Task Static_folder_unknown() {
            var request = await RestRequest("GET", "/folder_unknown");
            AssertRequest(request, 404, "text/plain", "list directory > folder not found: /folder_unknown");
        }
        
        // --------------------------------------- utils ---------------------------------------
        private static readonly string StaticAssets = CommonUtils.GetBasePath() + "assets~/Static/";

        private static void WriteStaticResponse(RequestContext request, string path) {
            File.WriteAllBytes(StaticAssets + path, request.Response.AsByteArray());
        }
    }
}

#endif
