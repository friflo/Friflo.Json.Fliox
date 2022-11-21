// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Test;
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
            var baseFolder  = CommonUtils.GetBasePath("./");
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
            var body            = QueryToStream(query, operationName, vars, out int bodyLength);
            var cookies         = CreateDefaultCookies();
            var headers         = new TestHttpHeaders(null, cookies);
            var requestContext  = new RequestContext(_httpHost, "POST", route, "", body, bodyLength, headers);
            // execute synchronous to enable tests running in Unity Test Runner
            _httpHost.ExecuteHttpRequest(requestContext).Wait();
            
            return requestContext;
        }
        
        private static Stream QueryToStream(string query, string operationName, string vars, out int streamLength) {
            var variables       = vars == null ? null : _mapper.reader.Read<Dictionary<string,JsonValue>>(vars);
            var request         = new GraphQLRequest { query = query, operationName = operationName, variables = variables };
            var jsonBody        = _mapper.writer.WriteAsArray(request);
            streamLength        = jsonBody.Length;
            var body            = new MemoryStream();
            body.Write(jsonBody, 0, streamLength);
            body.Flush();
            body.Position       = 0;
            return body;
        }
        
        
        // ------------------------------------------ REST ------------------------------------------
        private static void ExecuteHttpFile (string requestPath, string resultPath) {
            var httpFolder      = CommonUtils.GetBasePath() + "Common/UnitTest/Fliox/Remote/";
            var path            = httpFolder + requestPath;
            var content         = File.ReadAllText(path);
            var restFile        = new HttpFile(path, content);
            var sb              = new StringBuilder();
            restFile.AppendFileHeader(sb);
            foreach (var req in restFile.requests) {
                var stream      = req.GetBody(out var length);
                var context     = new RequestContext(_httpHost, req.method, req.path, req.query, stream, length, req.headers);
                // execute synchronous to enable tests running in Unity Test Runner
                _httpHost.ExecuteHttpRequest(context).Wait();
                
                HttpFile.AppendRequest(sb, context);
            }
            var fullResult      = httpFolder + resultPath;
            var result          = sb.ToString();
            File.WriteAllText(fullResult, result);
        }

        private static RequestContext HttpRequest(string method, string route, string query = "", string jsonBody = null)
        {
            var bodyStream      = HttpFileRequest.StringToStream(jsonBody, out int length);
            var cookies         = CreateDefaultCookies();
            var headers         = new TestHttpHeaders(null, cookies);
            var requestContext  = new RequestContext(_httpHost, method, route, query, bodyStream, length, headers);
            // execute synchronous to enable tests running in Unity Test Runner
            _httpHost.ExecuteHttpRequest(requestContext).Wait();
            
            return requestContext;
        }
        
        private static Dictionary<string, string> CreateDefaultCookies() {
            return new Dictionary<string, string> {
                { "fliox-user", "admin" },  { "fliox-token", "admin" }
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
        private readonly    Dictionary<string, string>  headers;
        private readonly    Dictionary<string, string>  cookies;
        
        public              string                      Header(string key) => headers.TryGetValue(key, out var value) ? value : null;
        public              string                      Cookie(string key) => cookies.TryGetValue(key, out var value) ? value : null;
        
        internal TestHttpHeaders (Dictionary<string, string> headers, Dictionary<string, string> cookies) {
            this.headers = headers ?? new Dictionary<string, string>();
            this.cookies = cookies;
        }
    }
}