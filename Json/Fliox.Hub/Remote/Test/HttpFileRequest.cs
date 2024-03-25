// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Remote.Test
{
    public sealed class HttpFileRequest
    {
        public  readonly    string              method;
        public  readonly    string              path;
        public  readonly    string              query;
        public  readonly    string              body;
        public  readonly    IHttpHeaders        headers;
        
        public              Stream              GetBody(out int length) => StringToStream(body, out length);

        public  override    string  ToString() {
            if (query == "")
                return $"{method} {path}"; 
            return $"{method} {path}?{query}";
        }

        private const           string  BaseVariable    = "{{base}}";
        private static readonly Regex   RegExVariables  = new Regex(@"\{{([^}]+)\}}", RegexOptions.Compiled);
        
        public HttpFileRequest (string requestText, HttpFile httpFile) {
            var parts       = requestText.Split(new [] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
            var head        = parts[0];
            body            = parts.Length > 1 ? parts[1] : null;
            var lines       = head.Split(new [] {"\r\n", "\n"}, StringSplitOptions.None);
            var requestLine = lines[0];
            
            var spacePos    = requestLine.IndexOf(' ');
            method          = requestLine.Substring(0, spacePos);
            var urlPath     = requestLine.Substring(spacePos + 1);
            var queryPos    = urlPath.IndexOf('?');
            path            = queryPos == -1 ? urlPath : urlPath.Substring(0, queryPos);
            var baseEnd     = path.IndexOf(BaseVariable, StringComparison.Ordinal);
            if (baseEnd == -1) {
                throw new InvalidOperationException("expect {{base}} in url. was: " +  urlPath);
            }
            path            = path.Substring(BaseVariable.Length);
            query           = queryPos == -1 ? "" : urlPath.Substring(queryPos + 1);
            var sb          = new StringBuilder();
            var headers     = ReadHeaders(lines, sb, httpFile);
            var cookies     = CreateCookies(headers);
            this.headers    = new HttpFileHeaders(headers, cookies); 
        }
        
        private static Dictionary<string, string> ReadHeaders(string[] lines, StringBuilder sb, HttpFile httpFile) {
            var result      = new Dictionary<string, string>();
            for (int n = 1; n < lines.Length; n++) {
                var line    = lines[n].Trim();
                if (line == "")
                    continue;
                var resultLine  = ReplaceVariables(line, sb, httpFile);
                var colonPos    = resultLine.IndexOf(':');
                var headerName  = resultLine.Substring(0, colonPos).Trim();
                var headerValue = resultLine.Substring(colonPos + 1).Trim();
                result.Add(headerName, headerValue);
            }            
            return result;
        }
        
        private static string ReplaceVariables(string line, StringBuilder sb, HttpFile httpFile)
        {
            var matches     = RegExVariables.Matches(line);
            if (matches.Count == 0)
                return line;
            var variables   = httpFile.variables;
            sb.Clear();
            var pos = 0;
            for (int i = 0; i < matches.Count; i++) {
                var match       = matches[i];
                var count       = match.Index - pos;
                sb.Append(line, pos, count);
                var matchValue  = match.Value;
                var variable    = matchValue.Substring(2, matchValue.Length - 4); // remove '{{' and '}}'
                var varValue    = variables[variable];
                sb.Append(varValue);
                pos            +=  count + match.Length;
            }
            return sb.ToString();
        }
        
        private static Dictionary<string, string> CreateCookies(Dictionary<string, string>  headers) {
            var result = new Dictionary<string, string> ();
            if (!headers.TryGetValue("Cookie", out var value))
                return result;
            var cookies = value.Split(new [] {";"}, StringSplitOptions.None);
            result.EnsureCapacity(cookies.Length);
            
            foreach (var cookie in cookies) {
                var assignPos   = cookie.IndexOf("=", StringComparison.Ordinal);
                var cookieName  = cookie.Substring(0, assignPos).Trim();
                var cookieValue = cookie.Substring(assignPos + 1).Trim();
                result.Add(cookieName, cookieValue);
            }
            return result;
        }
        
        public static Stream StringToStream(string jsonBody, out int length) {
            var bodyStream      = new MemoryStream();
            var writer          = new StreamWriter(bodyStream);
            writer.Write(jsonBody);
            writer.Flush();
            length              = (int)bodyStream.Position;
            bodyStream.Position = 0;
            return bodyStream;
        }
    }
    
    internal sealed class HttpFileHeaders : IHttpHeaders {
        private  readonly   Dictionary<string, string>  headers;
        public  readonly    Dictionary<string, string>  cookies;
        
        public              string                      Header(string key) => headers.TryGetValue(key, out var value) ? value : null;
        public              string                      Cookie(string key) => cookies.TryGetValue(key, out var value) ? value : null;
        
        public  HttpFileHeaders(Dictionary<string, string> headers, Dictionary<string, string> cookies) {
            this.headers = headers;
            this.cookies = cookies;
        }
    }
}