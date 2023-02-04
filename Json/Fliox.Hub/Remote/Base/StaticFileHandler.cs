// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Hub.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="StaticFileHandler"/> is used to serve static files by a <see cref="HttpHost"/>.
    /// </summary>
    /// <remarks>
    /// Therefore add an instance of <see cref="StaticFileHandler"/> with <see cref="HttpHost.AddHandler"/> to the Hub.<br/>
    /// The <b>Media Type</b> assigned to the response <c>Content-Type</c> header is derived from the file name extension.<br/>
    /// Add additional mappings for <b>file name extension</b> to <b>MediaType</b> with <see cref="AddFileExtension"/>.
    /// </remarks> 
    public sealed class StaticFileHandler : IRequestHandler
    {
        private  readonly   IFileHandler                                fileHandler;
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<string, CacheEntry>    cache; // concurrent access allowed
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<CacheEntry>                     Cache       => cache.Values;
        private             string                                      cacheControl;
        private  readonly   List<FileExt>                               fileExtensions;

        public   override   string                                      ToString()  => $"cached files: {cache.Count}";

        private StaticFileHandler() {
            cache           = new ConcurrentDictionary<string, CacheEntry>();
            cacheControl    = HttpHost.DefaultCacheControl;
            fileExtensions  = DefaultFileExtensions();
        }
        
        public StaticFileHandler (string rootFolder) : this() {
            fileHandler         = new FileHandler(rootFolder);
        }

        // e.g. new StaticFileHandler(wwwPath + ".zip", "www~"));
        public StaticFileHandler (string zipPath, string baseFolder) : this() {
            fileHandler = ZipFileHandler.Create(zipPath, baseFolder);
        }
        
        public StaticFileHandler (Stream zipStream, string baseFolder) : this() {
            fileHandler = new ZipFileHandler(zipStream, baseFolder);
        }
        
        public string CacheControl {
            get => cacheControl;
            set => cacheControl = value;
        }
        
        private static List<FileExt> DefaultFileExtensions() {
            var result = new List<FileExt> {
                new FileExt(".html",  "text/html; charset=UTF-8"),
                new FileExt(".js",    "application/javascript"),
                new FileExt(".png",   "image/png"),
                new FileExt(".css",   "text/css"),
                new FileExt(".svg",   "image/svg+xml"),
                new FileExt(".ico",   "image/x-icon"),
            };
            return result;
        }

        public void AddFileExtension(string  extension, string  mediaType) {
            var fileExt = new FileExt(extension, mediaType);
            fileExtensions.Add(fileExt);
        }
        
        public string[]  Routes => fileHandler.BaseFolders;
        
        public bool IsMatch(RequestContext context) {
            return context.method == "GET";
        }
            
        public async Task HandleRequest(RequestContext context) {
            try {
                if (cacheControl == null) {
                    await GetHandler(context).ConfigureAwait(false);
                    return;                    
                }
                if (cache.TryGetValue(context.route, out CacheEntry entry)) {
                    context.Write(entry.body, entry.mediaType, entry.status);
                    context.SetHeaders(entry.headers);
                    return;
                }
                await GetHandler(context).ConfigureAwait(false);
                if (cacheControl != null) {
                    context.AddHeader("Cache-Control", cacheControl); // seconds
                }
                if (context.StatusCode != 200)
                    return;
                var path = context.route;
                entry = new CacheEntry(path, context);
                cache[path] = entry;
            }
            catch (Exception e) {
                var response = $"method: {context.method}, url: {context.route}\n{e.Message}";
                context.WriteError("request exception", response, 500);
            }
        }
        
        private async Task GetHandler (RequestContext context) {
            var path = context.route;
            if (path.EndsWith("/")) {
                path += "index.html";
            }
            string ext = Path.GetExtension (path);
            if (string.IsNullOrEmpty(ext)) {
                ListDirectory(context);
                return;
            }
            var content = await fileHandler.ReadFile(path).ConfigureAwait(false);
            if (content == null) {
                context.WriteError("file error", $"file not found: {path}", 404);
                return;
            }
            var contentType = ContentTypeFromPath(path);
            var body        = new JsonValue(content);
            context.Write(body, contentType, 200);
        }
        
        private void ListDirectory (RequestContext context) {
            var folder = context.route;
            string[] fileNames = fileHandler.GetFiles(folder);
            if (fileNames == null) {
                var msg = $"folder not found: {folder}";
                context.WriteError("list directory", msg, 404);
                return;
            }
            Array.Sort(fileNames);
            using (var mapper = context.Pool.ObjectMapper.Get()) {
                var writer      = RemoteMessageUtils.GetPrettyWriter(mapper.instance);
                var jsonList    = writer.Write(fileNames);
                context.WriteString(jsonList, "application/json", 200);
            }
        }
        
        private string ContentTypeFromPath(string path) {
            foreach (var fileExt in fileExtensions) {
                if (path.EndsWith(fileExt.extension))
                    return fileExt.mediaType;
            }
            return "text/plain";
        }
    }
    
    internal readonly struct FileExt {
        internal  readonly  string  extension;
        internal  readonly  string  mediaType;
        
        internal  FileExt (string  extension, string  mediaType) {
            this.extension  = extension;
            this.mediaType  = mediaType;
        }
    }
    
    internal readonly struct CacheEntry {
        private   readonly  string                      path;
        internal  readonly  int                         status;
        internal  readonly  string                      mediaType;
        internal  readonly  JsonValue                   body; // any file content type. js, html, png, ... 
        internal  readonly  Dictionary<string, string>  headers;

        public    override  string                      ToString() => path;

        internal CacheEntry (string path, RequestContext context) {
            this.path   = path;
            status      = context.StatusCode;
            mediaType   = context.ResponseContentType;
            body        = context.Response;
            headers     = context.ResponseHeaders;
        }
    }
}
