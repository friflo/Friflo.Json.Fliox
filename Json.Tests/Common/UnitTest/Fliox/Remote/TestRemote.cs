// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Main;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public static partial class TestRemote
    {
        private static  HttpHostHub     _hostHub;   // todo - UserDB is bottleneck as it uses a file-system DB
        private static  ObjectMapper    _mapper;
        private static  SharedEnv       _env;
        
        [OneTimeSetUp]    public static void  Init() {
            var baseFolder  = CommonUtils.GetBasePath("../");
            _env            = new SharedEnv();
            _mapper         = new ObjectMapper(_env.TypeStore);
            // use NonConcurrent in-memory DB to preserve entity order of query results
            var config      = new Program.Config(_env, baseFolder, true, MemoryType.NonConcurrent);
            _hostHub        = Program.CreateHttpHost(config);
        }
        [OneTimeTearDown] public static void  Dispose() {
            _hostHub.Dispose();
            _mapper.Dispose();
            _env.Dispose();
        }
        
        
        // ----------------------------------------- GraphQL -----------------------------------------
        private static async Task<RequestContext> GraphQLRequest(string route, string query, string operationName = null)
        {
            var body            = QueryToStream(query, operationName);
            var headers         = new TestHttpHeaders();
            var cookies         = CreateCookies();
            var requestContext  = new RequestContext(_hostHub, "POST", route, "", body, headers, cookies);
            await _hostHub.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
            
            return requestContext;
        }
        
        private static Stream QueryToStream(string query, string operationName) {
            var request         = new GraphQLRequest { query = query, operationName = operationName };
            var jsonBody        = _mapper.writer.WriteAsArray(request);
            var body            = new MemoryStream();
            body.Write(jsonBody, 0, jsonBody.Length);
            body.Flush();
            body.Position       = 0;
            return body;
        }
        
        
        // ------------------------------------------ REST ------------------------------------------
        private static async Task<RequestContext> RestRequest(string method, string route, string query = "", string jsonBody = null)
        {
            var bodyStream      = BodyToStream(jsonBody);
            var headers         = new TestHttpHeaders();
            var cookies         = CreateCookies();
            var requestContext  = new RequestContext(_hostHub, method, route, query, bodyStream, headers, cookies);
            await _hostHub.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
            
            return requestContext;
        }
        
        private static Stream BodyToStream(string jsonBody) {
            var bodyStream      = new MemoryStream();
            var writer          = new StreamWriter(bodyStream);
            writer.Write(jsonBody);
            writer.Flush();
            bodyStream.Position   = 0;
            return bodyStream;
        }
        
        private static IHttpCookies CreateCookies() {
            return new TestHttpCookies {
                map = { ["fliox-user"]  = "admin",  ["fliox-token"] = "admin" }
            };
        }
        
        private static void AssertRequest(RequestContext request, int status, string contentType) {
            AreEqual(status,        request.StatusCode);
            AreEqual(contentType,   request.ResponseContentType);
        }

        private static void AssertRequest(RequestContext request, int status, string contentType, string response) {
            AreEqual(status,        request.StatusCode);
            AreEqual(contentType,   request.ResponseContentType);
            AreEqual(response,      request.Response.AsString());
        }

    }
    
    internal class GraphQLRequest {
        public  string                      query;
        public  string                      operationName;
    //  public  Dictionary<string,string>   variables;
    }
    
    internal class TestHttpHeaders : IHttpHeaders {
    //  public  readonly    Dictionary<string, string>  map = new Dictionary<string, string>();
        
        public              string                      this[string key] => null;
    }
    
    internal class TestHttpCookies : IHttpCookies {
        public  readonly    Dictionary<string, string>  map = new Dictionary<string, string>();
        
        public              string                      this[string key] => map[key];
    }
}