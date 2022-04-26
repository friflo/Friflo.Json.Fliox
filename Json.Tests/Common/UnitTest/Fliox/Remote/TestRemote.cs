// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Main;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public static partial class TestRemote
    {
        private static readonly string TestFolder = CommonUtils.GetBasePath() + "assets~/GraphQL/";
        
        private static HttpHostHub _hostHub;
        
        [OneTimeSetUp]    public static void  Init() {
            var baseFolder  = CommonUtils.GetBasePath("../");
            _hostHub         = Program.CreateHttpHost(baseFolder);
        }
        [OneTimeTearDown] public static void  Dispose() {
            _hostHub.Dispose();
        }
            
        private static async Task<RequestContext> GraphQLRequest(string route, string query)
        {
            var request     = new GraphQLRequest { query = query };
            var jsonBody    = JsonSerializer.SerializeAsArray(request);
            var body        = new System.IO.MemoryStream();
            await body.WriteAsync(jsonBody, 0, jsonBody.Length);
            body.Position   = 0;
            
            var headers = new TestHttpHeaders();
            var cookies = new TestHttpCookies {
                map = {
                    ["fliox-user"]  = "admin",
                    ["fliox-token"] = "admin",
                }
            };
            var requestContext = new RequestContext(_hostHub, "POST", route, "", body, headers, cookies);
            await _hostHub.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
            
            return requestContext;
        }
    }
    
    internal class GraphQLRequest {
        public  string                      query;
    //  public  string                      operationName;
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