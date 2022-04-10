// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// A <see cref="StaticFileHandler"/> is used to serve static files by a <see cref="HttpHostHub"/>.<br/>
    /// Therefore add an instance of <see cref="StaticFileHandler"/> with <see cref="HttpHostHub.AddHandler"/> to the Hub.<br/>
    /// The <b>Media Type</b> assigned to the response <c>Content-Type</c> header is derived from the file name extension.<br/>
    /// Add additional mappings for <b>file name extension</b> to <b>MediaType</b> with <see cref="AddFileExtension"/>. 
    /// </summary>
    public class StaticFileHandler : IRequestHandler
    {
        private readonly    IFileHandler                    fileHandler;
        private readonly    Dictionary<string, CacheEntry>  cache = new Dictionary<string, CacheEntry>();
        private             string                          cacheControl    = HttpHostHub.DefaultCacheControl;
        
        private readonly List<FileExt>  fileExtensions = new List<FileExt> {
            new FileExt(".html",  "text/html; charset=UTF-8"),
            new FileExt(".js",    "application/javascript"),
            new FileExt(".png",   "image/png"),
            new FileExt(".css",   "text/css"),
            new FileExt(".svg",   "image/svg+xml"),
            new FileExt(".ico",   "image/x-icon"),
        };
        
        public StaticFileHandler (string rootFolder) {
            fileHandler         = new FileHandler(rootFolder);
        }

        // e.g. new StaticFileHandler(wwwPath + ".zip", "www~"));
        public StaticFileHandler (string zipPath, string baseFolder) {
            fileHandler = ZipFileHandler.Create(zipPath, baseFolder);
        }
        
        public StaticFileHandler (Stream zipStream, string baseFolder) {
            fileHandler = new ZipFileHandler(zipStream, baseFolder);
        }
        
        public StaticFileHandler CacheControl(string cacheControl) {
            this.cacheControl   = cacheControl;
            return this;
        }

        public void AddFileExtension(string  extension, string  mediaType) {
            var fileExt = new FileExt(extension, mediaType);
            fileExtensions.Add(fileExt);
        }
        
        public string  Route => "/* - static files";
        
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
                    var body = new JsonValue(entry.body);
                    context.Write(body, 0, entry.mediaType, entry.status);
                    context.SetHeaders(entry.headers);
                    return;
                }
                await GetHandler(context).ConfigureAwait(false);
                if (cacheControl != null) {
                    context.AddHeader("Cache-Control", cacheControl); // seconds
                }
                if (context.StatusCode != 200)
                    return;
                entry = new CacheEntry(context);
                cache.Add(context.route, entry);
            }
            catch (Exception e) {
                var response = $"method: {context.method}, url: {context.route}\n{e.Message}";
                context.WriteError("request exception", response, 500);
            }
        }
        
        private async Task GetHandler (RequestContext context) {
            var path = context.route;
            if (path.EndsWith("/"))
                path += "index.html";
            string ext = Path.GetExtension (path);
            if (string.IsNullOrEmpty(ext)) {
                ListDirectory(context);
                return;
            }
            var content = await fileHandler.ReadFile(path).ConfigureAwait(false);
            var contentType = ContentTypeFromPath(path);
            context.Write(new JsonValue(content), 0, contentType, 200);
        }
        
        private void ListDirectory (RequestContext context) {
            var folder = context.route;
            string[] fileNames = fileHandler.GetFiles(folder);
            if (fileNames == null) {
                var msg = $"folder not found: {folder}";
                context.WriteError("list directory", msg, 404);
                return;
            }
            var options = new SerializerOptions{ Pretty = true };
            var jsonList = JsonSerializer.Serialize(fileNames, options);
            context.WriteString(jsonList, "application/json", 200);
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
        internal  readonly  int                         status;
        internal  readonly  string                      mediaType;
        internal  readonly  byte[]                      body;
        internal  readonly  Dictionary<string, string>  headers;
        
        internal CacheEntry (RequestContext context) {
            status      = context.StatusCode;
            mediaType   = context.ResponseContentType;
            body        = context.Response.AsByteArray();
            headers     = context.ResponseHeaders;
        }
    }
}
