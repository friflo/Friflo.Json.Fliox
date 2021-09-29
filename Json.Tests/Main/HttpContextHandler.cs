#if !UNITY_2020_1_OR_NEWER

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Main
{
    public class HttpContextHandler : IHttpContextHandler
    {
        private readonly string wwwRoot;
        
        public HttpContextHandler (string wwwRoot) {
            this.wwwRoot = wwwRoot;    
        }
            
        public async Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase) {
            var req = context.Request;
            var resp = context.Response;
            try {
                if (req.HttpMethod == "GET") {
                    await GetHandler(req, resp);
                    resp.Close();
                    return true;
                }
            }
            catch (Exception ) {
                var     response        = $"error: method: {req.HttpMethod}, url: {req.Url.AbsolutePath}";
                byte[]  responseBytes   = Encoding.UTF8.GetBytes(response);
                HttpHostDatabase.SetResponseHeader(resp, "text/plain", HttpStatusCode.BadRequest, responseBytes.Length);
                await resp.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                resp.Close();
            }
            return true;
        }
        
        private async Task<bool> GetHandler (HttpListenerRequest req,  HttpListenerResponse resp) {
            var path = req.Url.AbsolutePath;
            if (path.EndsWith("/"))
                path += "index.html";
            string ext = Path.GetExtension (path);
            if (string.IsNullOrEmpty(ext)) {
                return await ListDirectory(req, resp);
            }
            var filePath = wwwRoot + path;
            var content = await ReadFile(filePath).ConfigureAwait(false);
            var contentType = ContentTypeFromPath(path);
            HttpHostDatabase.SetResponseHeader(resp, contentType, HttpStatusCode.OK, content.Length);
            await resp.OutputStream.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
            return true;
        }
        
        private async Task<bool> ListDirectory (HttpListenerRequest req,  HttpListenerResponse resp) {
            var result = new MemoryStream();
            using (var writer = new StreamWriter(result) { AutoFlush = true }) {
                var path = wwwRoot + req.Url.AbsolutePath;
                if (!Directory.Exists(path)) {
                    writer.Write($"directory doesnt exist: {req.Url.AbsolutePath}");
                    HttpHostDatabase.SetResponseHeader(resp, "text/plain", HttpStatusCode.OK, (int)result.Length);
                    await resp.OutputStream.WriteAsync(result.ToArray(), 0, (int)result.Length).ConfigureAwait(false);
                    return false;
                }
                string[] fileNames = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                for (int n = 0; n < fileNames.Length; n++) {
                    fileNames[n] = fileNames[n].Substring(wwwRoot.Length).Replace('\\', '/');
                }
                var jsonList = JsonDebug.ToJson(fileNames, true);
                writer.Write(jsonList);
                HttpHostDatabase.SetResponseHeader(resp, "application/json", HttpStatusCode.OK, (int)result.Length);
                await resp.OutputStream.WriteAsync(result.ToArray(), 0, (int)result.Length).ConfigureAwait(false);
                return true;
            }
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
