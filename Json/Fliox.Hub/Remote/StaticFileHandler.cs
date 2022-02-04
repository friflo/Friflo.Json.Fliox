// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public class StaticFileHandler : IRequestHandler
    {
        private readonly    IFileHandler                    fileHandler;
        private readonly    Dictionary<string, CacheEntry>  cache = new Dictionary<string, CacheEntry>();
        private readonly    bool                            cacheResponses;
        
        private readonly List<FileExt>  fileExtensions = new List<FileExt> {
            new FileExt(".html",  "text/html; charset=UTF-8"),
            new FileExt(".js",    "application/javascript"),
            new FileExt(".png",   "image/png"),
            new FileExt(".css",   "text/css"),
            new FileExt(".svg",   "image/svg+xml"),
            new FileExt(".ico",   "image/x-icon"),
        };
        
        public StaticFileHandler (string rootFolder, bool cacheResponses = true) {
            fileHandler         = new FileHandler(rootFolder);
            this.cacheResponses = cacheResponses;
        }
        
        // e.g. new StaticFileHandler(wwwPath + ".zip", "www~"));
        public StaticFileHandler (string zipPath, string baseFolder) {
            fileHandler = ZipFileHandler.Create(zipPath, baseFolder);
        }
        
        public StaticFileHandler (Stream zipStream, string baseFolder) {
            fileHandler = new ZipFileHandler(zipStream, baseFolder);
        }

        public void AddFileExtension(string  extension, string  mediaType) {
            var fileExt = new FileExt(extension, mediaType);
            fileExtensions.Add(fileExt);
        }
        
        public bool IsMatch(RequestContext context) {
            return context.method == "GET";
        }
            
        public async Task HandleRequest(RequestContext context) {
            try {
                if (!cacheResponses) {
                    await GetHandler(context);
                    return;                    
                }
                if (cache.TryGetValue(context.path, out CacheEntry entry)) {
                    var body = new JsonValue(entry.body);
                    context.Write(body, 0, entry.mediaType, entry.status);
                    context.SetHeaders(entry.headers);
                    return;
                }
                await GetHandler(context);
                context.AddHeader("Cache-Control", "max-age=600"); // seconds
                if (context.StatusCode != 200)
                    return;
                entry = new CacheEntry(context);
                cache.Add(context.path, entry);
            }
            catch (Exception ) {
                var response = $"method: {context.method}, url: {context.path}";
                context.WriteError("request exception", response, 500);
            }
        }
        
        private async Task GetHandler (RequestContext context) {
            var path = context.path;
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
            var folder = context.path;
            string[] fileNames = fileHandler.GetFiles(folder);
            if (fileNames == null) {
                var msg = $"folder not found: {folder}";
                context.WriteError("list directory", msg, 404);
                return;
            }
            var options = new SerializerOptions{ Pretty = true };
            var jsonList = JsonSerializer.Serialize(fileNames, options);
            context.WriteString(jsonList, "application/json");
        }
        
        private string ContentTypeFromPath(string path) {
            foreach (var fileExt in fileExtensions) {
                if (path.EndsWith(fileExt.extension))
                    return fileExt.mediaType;
            }
            return "text/plain";
        }
    }
    
    public readonly struct FileExt {
        public readonly     string  extension;
        public readonly     string  mediaType;
        
        public FileExt (string  extension, string  mediaType) {
            this.extension  = extension;
            this.mediaType  = mediaType;
        }
    }
    
    public readonly struct CacheEntry {
        public readonly     int                         status;
        public readonly     string                      mediaType;
        public readonly     byte[]                      body;
        public readonly     Dictionary<string, string>  headers;
        
        public CacheEntry (RequestContext context) {
            status      = context.StatusCode;
            mediaType   = context.ResponseContentType;
            body        = context.Response.AsByteArray();
            headers     = context.ResponseHeaders;
        }
    }
}
