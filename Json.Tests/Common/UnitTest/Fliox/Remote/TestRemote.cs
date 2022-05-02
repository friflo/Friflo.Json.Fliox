// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private static  HttpHost        _httpHost;   // todo - UserDB is bottleneck as it uses a file-system DB
        private static  ObjectMapper    _mapper;
        private static  SharedEnv       _env;
        
        [OneTimeSetUp]    public static void  Init() {
            var baseFolder  = CommonUtils.GetBasePath("../");
            _env            = new SharedEnv();
            _mapper         = new ObjectMapper(_env.TypeStore);
            // use NonConcurrent in-memory DB to preserve entity order of query results
            var config      = new Program.Config(_env, baseFolder, true, MemoryType.NonConcurrent, "max-age=600");
            _httpHost       = Program.CreateHttpHost(config);
        }
        [OneTimeTearDown] public static void  Dispose() {
            _httpHost.Dispose();
            _mapper.Dispose();
            _env.Dispose();
        }
        
        
        // ----------------------------------------- GraphQL -----------------------------------------
        private static RequestContext GraphQLRequest(string route, string query, string vars = null, string operationName = null)
        {
            var body            = QueryToStream(query, operationName, vars);
            var headers         = new TestHttpHeaders();
            var cookies         = CreateCookies();
            var requestContext  = new RequestContext(_httpHost, "POST", route, "", body, headers, cookies);
            // execute synchronous to enable tests running in Unity Test Runner
            _httpHost.ExecuteHttpRequest(requestContext).Wait();
            
            return requestContext;
        }
        
        private static Stream QueryToStream(string query, string operationName, string vars) {
            var variables       = vars == null ? null : _mapper.reader.Read<Dictionary<string,JsonValue>>(vars);
            var request         = new GraphQLRequest { query = query, operationName = operationName, variables = variables };
            var jsonBody        = _mapper.writer.WriteAsArray(request);
            var body            = new MemoryStream();
            body.Write(jsonBody, 0, jsonBody.Length);
            body.Flush();
            body.Position       = 0;
            return body;
        }
        
        
        // ------------------------------------------ REST ------------------------------------------
        private static void ExecuteHttpFile (string requestPath, string resultPath) {
            var fullRequest     = CommonUtils.GetBasePath() + "assets~/Remote/" + requestPath;
            var restFile        = HttpFile.Read(fullRequest);
            var sb              = new StringBuilder();
            sb.AppendLine("@base = http://localhost:8010/fliox");
            sb.AppendLine();
            foreach (var req in restFile.requests) {
                var context = HttpRequest(req.method, req.path, req.query, req.body);
                HttpFile.AppendRequest(sb, context);
            }
            var fullResult      = CommonUtils.GetBasePath() + "assets~/Remote/" + resultPath;
            var result          = sb.ToString();
            File.WriteAllText(fullResult, result);
        }

        private static RequestContext HttpRequest(string method, string route, string query = "", string jsonBody = null)
        {
            var bodyStream      = BodyToStream(jsonBody);
            var headers         = new TestHttpHeaders();
            var cookies         = CreateCookies();
            var requestContext  = new RequestContext(_httpHost, method, route, query, bodyStream, headers, cookies);
            // execute synchronous to enable tests running in Unity Test Runner
            _httpHost.ExecuteHttpRequest(requestContext).Wait();
            
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
        public  string                          query;
        public  string                          operationName;
        public  Dictionary<string, JsonValue>   variables;
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