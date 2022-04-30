// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    /// <summary>
    /// <see cref="HttpFile"/> is used to run multiple http requests specified in a simple text file. <br/>
    /// The text file format is compatible to:
    /// <a href="https://marketplace.visualstudio.com/items?itemName=humao.rest-client">REST Client - Visual Studio Marketplace</a>
    /// <br/>
    /// This approach enables request execution:
    /// - automated by unit tests
    /// - individually using an IDE - e.g. the mentioned REST Client
    /// <br/>
    /// Another benefit of this approach is to avoid flooding the test suite with primitive tests.
    /// <br/>
    /// This class is utilized by <see cref="TestRemote.ExecuteHttpFile"/> and create a single output file for the given request file.
    /// The output files are added to version control. The expectation is that after running the tests the output files are
    /// "unmodified" in version control (Git). <br/>
    /// If an output file is "modified" its new version have to be added to version control in case the response meets the expectation. 
    /// </summary>
    public class HttpFile
    {
        private  readonly   string              path;
        private  readonly   List <HttpRequest>  requests;

        public  override    string              ToString() => path;

        public HttpFile(string path, string content) {
            this.path   = path;
            requests    = new List <HttpRequest>();
            
            var sections = content.Split(new [] {"###\r\n"}, StringSplitOptions.None);
            // skip first entry containing variables
            for (int n = 1; n < sections.Length; n++) {
                var section = sections[n];
                var request = new HttpRequest(section);
                requests.Add(request);
            }
        }
        
        public static HttpFile Read(string path) {
            var content = File.ReadAllText(path);
            return new HttpFile(path, content);
        }
        
        public void Execute(StringBuilder sb) {
            sb.AppendLine("@base = http://localhost:8010/fliox");
            sb.AppendLine();
            foreach (var req in requests) {
                var context = TestRemote.RestRequest(req.method, req.path, req.query, req.body);
                
                sb.AppendLine("###");
                // --- request line
                sb.Append(req.method);
                sb.Append(' ');
                sb.Append("{{base}}");
                sb.Append(req.path);
                if (req.query != "") {
                    sb.Append('?');
                    sb.Append(req.query);
                }
                sb.AppendLine();
                
                // --- StatusCode
                sb.Append("# StatusCode:   ");
                sb.Append(context.StatusCode);
                sb.AppendLine();
                
                // --- Content-Type
                sb.Append("# Content-Type: ");
                sb.Append(context.ResponseContentType);
                sb.AppendLine();
                
                // --- response body
                sb.AppendLine();
                var responseBody = context.Response.AsString();
                sb.AppendLine(responseBody);
                sb.AppendLine();
            }
        }
    }
    
    public class HttpRequest
    {
        public  readonly    string  method;
        public  readonly    string  path;
        public  readonly    string  query;
        public  readonly    string  body;

        public  override    string  ToString() {
            if (query == "")
                return $"{method} {path}"; 
            return $"{method} {path}?{query}";
        }

        private const   string  BaseVariable = "{{base}}";
        
        public HttpRequest (string request) {
            var parts       = request.Split(new [] { "\r\n\r\n" }, StringSplitOptions.None);
            var head        = parts[0];
            body            = parts.Length > 1 ? parts[1] : null;
            var headers     = head.Split(new [] {"\r\n"}, StringSplitOptions.None);
            var requestLine = headers[0];
            
            var spacePos    = requestLine.IndexOf(' ');
            method          = requestLine.Substring(0, spacePos);
            var urlPath     = requestLine.Substring(spacePos + 1);
            var queryPos    = urlPath.IndexOf('?');
            path            = queryPos == -1 ? urlPath : urlPath.Substring(0, queryPos);
            var baseEnd     = path.IndexOf(BaseVariable, StringComparison.InvariantCulture);
            if (baseEnd == -1) {
                throw new InvalidEnumArgumentException("expect {{base}} in url. was: " +  urlPath);
            }
            path            = path.Substring(BaseVariable.Length);
            query           = queryPos == -1 ? "" : urlPath.Substring(queryPos + 1);
        }
    }
}