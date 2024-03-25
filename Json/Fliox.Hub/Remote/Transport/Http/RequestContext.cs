// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IRequestHandler
    {
        /// <summary>
        /// The handled routes of an <see cref="IRequestHandler"/> implementation  
        /// </summary>
        string[]    Routes          { get; }
        /// <summary>
        /// Return true if request can be handled by <see cref="IRequestHandler"/> implementation
        /// </summary>
        bool        IsMatch         (RequestContext context);
        /// <summary>
        /// return true if request was handled.
        /// Otherwise false to enable subsequent handlers processing the request  
        /// </summary>
        Task<bool>  HandleRequest   (RequestContext context);
    }
    
    /// <summary>
    /// A <see cref="RequestContext"/> is used to get the data of a HTTP request from a specific HTTP server
    /// and provide its execution results back to the HTTP server.    
    /// </summary>
    public sealed class RequestContext
    {
        // --- fields
        public    readonly  FlioxHub                    hub;
        public    readonly  IHost                       host;
        public    readonly  string                      method;
        public    readonly  string                      route;
        public    readonly  string                      query;
        public    readonly  Stream                      body;
        public    readonly  int                         contentLength;
        public    readonly  IHttpHeaders                headers;
        private             Dictionary<string, string>  responseHeaders;
        internal            bool                        handled;
        public    readonly  MemoryBuffer                memoryBuffer;
        // --- public properties
        public              string                      ResponseContentType { get; private set; }
        public              bool                        ResponseGzip        { get; internal set; }
        public              int                         StatusCode          { get; private set; }
        public              JsonValue                   Response            { get; private set; }
        public              Dictionary<string, string>  ResponseHeaders     => responseHeaders;
        public              bool                        Handled             => handled;
        public              ObjectPool<ObjectMapper>    ObjectMapper        => Pool.ObjectMapper;
        // --- internal properties
        internal            Pool                        Pool                => hub.sharedEnv.pool;
        internal            SharedCache                 SharedCache         => hub.sharedEnv.sharedCache;

        public    override  string                      ToString()          => $"{method} {route}{query}";

        public RequestContext (
            HttpHost        host,
            string          method,
            string          route,
            string          query,
            Stream          body,
            int             contentLength,
            IHttpHeaders    headers,
            MemoryBuffer    memoryBuffer)
        {
            this.hub            = host.hub;
            this.host           = host;
            this.method         = method;
            this.route          = route;
            this.query          = query;
            this.body           = body;
            this.contentLength  = contentLength;
            this.headers        = headers;
            this.memoryBuffer   = memoryBuffer;
        }
        
        public void Write (in JsonValue value, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = value;
        }
        
        public void WriteString (string value, string contentType, int statusCode) {
            ResponseContentType = contentType;
            StatusCode          = statusCode;
            Response            = new JsonValue(value);
        }
        
        public void WriteError (string errorType, string message, int statusCode) {
            var error           = $"{errorType} > {message}";
            ResponseContentType = "text/plain";
            StatusCode          = statusCode;
            Response            = new JsonValue(error);
        }
        
        public void AddHeader(string key, string value) {
            if (responseHeaders == null) {
                responseHeaders = new Dictionary<string, string>();
            }
            responseHeaders.Add(key, value);
        }
        
        public void SetHeaders(Dictionary<string, string> headers) {
            responseHeaders = headers;
        }
        
        public static bool IsBasePath(string basePath, string route) {
            if (!route.StartsWith(basePath))
                return false;
            if (route.Length == basePath.Length)
                return true;
            return route[basePath.Length] == '/';
        }
    }
    
    public interface IHttpHeaders {
        string  Header(string key);
        string  Cookie(string key);
    }
}