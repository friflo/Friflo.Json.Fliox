// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Remote.Test
{
    /// <summary>
    /// <see cref="HttpFile"/> is used to run multiple HTTP requests given in a simple text file used for concise regression tests
    /// </summary>
    /// <remarks>
    /// The text file format is compatible to:
    /// <a href="https://marketplace.visualstudio.com/items?itemName=humao.rest-client">REST Client - Visual Studio Marketplace</a>
    /// <br/>
    /// This approach enables request execution in multiple ways:
    /// <list type="bullet">
    ///   <item> automated by unit tests </item>
    ///   <item> individually by using an IDE - e.g. the mentioned REST Client or Rider </item>
    /// </list>
    /// Other benefits using this approach:
    /// <list type="bullet">
    ///   <item>
    ///     avoid flooding the test suite with primitive tests
    ///   </item>
    ///   <item>
    ///     simplify adjusting test results in case of behavior changes (e.g. bugfix or enhancements) by simplify
    ///     committing the modified request response to version control.
    ///   </item>
    ///   <item>
    ///     avoid writing test assertions using difficult string literals caused by escaped characters or line feeds
    ///   </item>
    /// </list>
    /// This class is utilized by <b>Friflo.Json.Tests.Common.UnitTest.Fliox.Remote.TestRemote</b>
    /// to write a single output file for a given request file.
    /// <br/>
    /// The generated output files are intended to be added to version control.
    /// The expectation is that after running the tests the output files are <b>unmodified</b> in version control (Git). <br/>
    /// In case an output file is <b>modified</b> its new version have to be added to version control if the modifications meets expectation.
    /// </remarks> 
    public sealed class HttpFile
    {
        private  readonly   string                      path;
        public   readonly   List <HttpFileRequest>      requests;
        public   readonly   Dictionary<string, string>  variables;

        public  override    string                      ToString() => path;

        public HttpFile(string path, string content) {
            this.path       = path;
            requests        = new List <HttpFileRequest>();
            var sections    = content.Split(new [] {"###\r\n", "###\n"}, StringSplitOptions.None);
            variables       = ReadVariables(sections[0]);
            
            // skip first entry containing variables
            for (int n = 1; n < sections.Length; n++) {
                var section = sections[n];
                var request = new HttpFileRequest(section, this);
                requests.Add(request);
            }
        }
        
        private static Dictionary<string, string> ReadVariables (string head) {
            var result  = new Dictionary<string, string>();
            var lines   = head.Split(new [] {"\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in lines) {
                if (!line.StartsWith("@"))
                    continue;
                var assignPos = line.IndexOf('=', 1);
                if (assignPos == -1)
                    continue;
                var name    = line.Substring(1, assignPos - 1).Trim(); 
                var value   = line.Substring(assignPos + 1).Trim();
                result.Add(name, value);
            }
            return result;
        }

        public void AppendFileHeader(StringBuilder sb) {
            foreach (var pair in variables) {
                sb.Append('@');
                sb.Append(pair.Key);
                sb.Append(" = ");
                sb.AppendLF(pair.Value);
            }
            sb.AppendLF();
        }
        
        public static void AppendRequest(StringBuilder sb, RequestContext context)
        {
            sb.AppendLF("###");
            // --- request line
            sb.Append(context.method);
            sb.Append(' ');
            sb.Append("{{base}}");
            sb.Append(context.route);
            if (context.query != "") {
                sb.Append('?');
                sb.Append(context.query);
            }
            sb.AppendLF();
            
            // --- StatusCode
            sb.Append("# StatusCode:   ");
            sb.Append(context.StatusCode);
            sb.AppendLF();
            
            // --- Content-Type
            sb.Append("# Content-Type: ");
            sb.Append(context.ResponseContentType);
            sb.AppendLF();
            
            // --- response body
            sb.AppendLF();
            var responseBody = context.Response.AsString();
            sb.AppendLF(responseBody);
            sb.AppendLF();
        }
    }
}