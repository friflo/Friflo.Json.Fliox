#if !UNITY_2020_1_OR_NEWER

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Main
{
    public class RequestHandler : IRequestHandler
    {
        private readonly string wwwRoot;
        
        public RequestHandler (string wwwRoot) {
            this.wwwRoot = wwwRoot;    
        }
            
        public async Task<bool> HandleRequest(RequestContext context) {
            try {
                if (context.method == "GET") {
                    await GetHandler(context);
                }
            }
            catch (Exception ) {
                var response = $"error - method: {context.method}, url: {context.url.AbsolutePath}";
                context.WriteString(response, "text/plain", HttpStatusCode.OK);
            }
            return true; // return true to signal request was handled -> no subsequent handlers are invoked 
        }
        
        private async Task GetHandler (RequestContext context) {
            var path = context.url.AbsolutePath;
            if (path.EndsWith("/"))
                path += "index.html";
            string ext = Path.GetExtension (path);
            if (string.IsNullOrEmpty(ext)) {
                ListDirectory(context);
                return;
            }
            var filePath = wwwRoot + path;
            var content = await ReadFile(filePath).ConfigureAwait(false);
            var contentType = ContentTypeFromPath(path);
            context.Write(content, 0, content.Length, contentType, HttpStatusCode.OK);
        }
        
        private void ListDirectory (RequestContext context) {
            var path = wwwRoot + context.url.AbsolutePath;
            if (!Directory.Exists(path)) {
                var msg = $"directory doesnt exist: {path}";
                context.WriteString(msg, "text/plain", HttpStatusCode.NotFound);
                return;
            }
            string[] fileNames = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            for (int n = 0; n < fileNames.Length; n++) {
                fileNames[n] = fileNames[n].Substring(wwwRoot.Length).Replace('\\', '/');
            }
            var jsonList = JsonDebug.ToJson(fileNames, true);
            context.WriteString(jsonList, "application/json", HttpStatusCode.OK);
        }
        
        private static string ContentTypeFromPath(string path) {
            if (path.EndsWith(".html"))
                return "text/html; charset=UTF-8";
            if (path.EndsWith(".js"))
                return "application/javascript";
            if (path.EndsWith(".png"))
                return "image/png";
            if (path.EndsWith(".css"))
                return "text/css";
            if (path.EndsWith(".svg"))
                return "image/svg+xml";
            return "text/plain";
        }
        
        private static async Task<byte[]> ReadFile(string filePath) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var memoryStream = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0) {
                    memoryStream.Write(buffer, 0, numRead);
                }
                return memoryStream.ToArray();
            }
        }
    }
}

#endif
