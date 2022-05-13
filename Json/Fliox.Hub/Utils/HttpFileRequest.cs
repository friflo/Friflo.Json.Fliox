// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Friflo.Json.Fliox.Hub.Remote;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Utils
{
    public class HttpFileRequest
    {
        public  readonly    string              method;
        public  readonly    string              path;
        public  readonly    string              query;
        public  readonly    string              body;
        public  readonly    HttpFileHeaders     headers;
        public  readonly    HttpFileCookies     cookies;
        
        public              Stream              BodyStream => StringToStream(body);

        public  override    string  ToString() {
            if (query == "")
                return $"{method} {path}"; 
            return $"{method} {path}?{query}";
        }

        private const           string  BaseVariable    = "{{base}}";
        private static readonly Regex   RegExVariables  = new Regex(@"\{{([^}]+)\}}");
        
        public HttpFileRequest (string request, HttpFile httpFile) {
            var parts       = request.Split(new [] { "\r\n\r\n" }, StringSplitOptions.None);
            var head        = parts[0];
            body            = parts.Length > 1 ? parts[1] : null;
            var lines       = head.Split(new [] {"\r\n"}, StringSplitOptions.None);
            var requestLine = lines[0];
            
            var spacePos    = requestLine.IndexOf(' ');
            method          = requestLine.Substring(0, spacePos);
            var urlPath     = requestLine.Substring(spacePos + 1);
            var queryPos    = urlPath.IndexOf('?');
            path            = queryPos == -1 ? urlPath : urlPath.Substring(0, queryPos);
            var baseEnd     = path.IndexOf(BaseVariable, StringComparison.InvariantCulture);
            if (baseEnd == -1) {
                throw new InvalidOperationException("expect {{base}} in url. was: " +  urlPath);
            }
            path            = path.Substring(BaseVariable.Length);
            query           = queryPos == -1 ? "" : urlPath.Substring(queryPos + 1);
            var headerMap   = ReadHeaders(lines, httpFile);
            headers         = new HttpFileHeaders(headerMap); 
            cookies         = CreateCookies(headerMap);
        }
        
        private static Dictionary<string, string> ReadHeaders(string[] lines, HttpFile httpFile) {
            var result      = new Dictionary<string, string>();
            var sb          = new StringBuilder();
            var variables   = httpFile.variables;
            
            for (int n = 1; n < lines.Length; n++) {
                sb.Clear();
                var line    = lines[n].Trim();
                if (line == "")
                    continue;
                var matches = RegExVariables.Matches(line);
                var pos = 0;
                for (int i = 0; i < matches.Count; i++) {
                    var match       = matches[i];
                    var count       = match.Index - pos;
                    sb.Append(line, pos, count);
                    var variable    = match.Value;
                    var varValue    = variables[variable]; 
                    sb.Append(varValue);
                    pos            +=  count + match.Length;
                }
                var resultLine  = sb.ToString();
                var colonPos    = resultLine.IndexOf(':');
                var name        = resultLine.Substring(0, colonPos).Trim();
                var value       = resultLine.Substring(colonPos + 1).Trim();
                result.Add(name, value);
            }            
            return result;
        }
        
        private static HttpFileCookies CreateCookies(Dictionary<string, string>  headers) {
            var result = new HttpFileCookies ();
            if (!headers.TryGetValue("Cookie", out var value))
                return result;
            var cookies = value.Split(new [] {";"}, StringSplitOptions.None);
            result.cookies.EnsureCapacity(cookies.Length);
            
            foreach (var cookie in cookies) {
                var assignPos   = cookie.IndexOf("=", StringComparison.InvariantCulture);
                var cookieName  = cookie.Substring(0, assignPos).Trim();
                var cookieValue = cookie.Substring(assignPos + 1).Trim();
                result.cookies.Add(cookieName, cookieValue);
            }
            return result;
        }
        
        public static Stream StringToStream(string jsonBody) {
            var bodyStream      = new MemoryStream();
            var writer          = new StreamWriter(bodyStream);
            writer.Write(jsonBody);
            writer.Flush();
            bodyStream.Position   = 0;
            return bodyStream;
        }
    }
    
    public class HttpFileHeaders : IHttpHeaders {
        private  readonly   Dictionary<string, string>  headers;
        public              string                      this[string key] => headers.TryGetValue(key, out var value) ? value : null;
        
        public  HttpFileHeaders(Dictionary<string, string> headers) {
            this.headers = headers;
        }
    }
    
    public class HttpFileCookies : IHttpCookies {
        public  readonly    Dictionary<string, string>  cookies = new Dictionary<string, string>();
        
        public              string                      this[string key] => cookies.TryGetValue(key, out var value) ? value : null;
    }
}