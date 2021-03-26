// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Mapper;


namespace Friflo.Json.EntityGraph.Database
{
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    public class RemoteHost : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    JsonMapper      jsonMapper;
        private readonly    string          endpoint;
        private readonly    HttpListener    listener;
        
        private             int             requestCount;
        
        public RemoteHost(EntityDatabase local, string endpoint) {
            this.endpoint = endpoint;
            listener = new HttpListener();
            listener.Prefixes.Add(endpoint);
            jsonMapper = new JsonMapper();
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, database);
            HostContainer container = new HostContainer(name, this, localContainer);
            return container;
        }

        public override SyncResponse Execute(SyncRequest syncRequest) {
            var response = new SyncResponse();
            return response;
        }
        
        private async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest  req  = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                string reqMsg = $@"request #: {+requestCount}
{req.Url.ToString()}
{req.HttpMethod}
{req.UserHostName}
{req.UserAgent}";
                Log(reqMsg);

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if (req.HttpMethod == "POST")
                {
                    if (req.Url.AbsolutePath == "/shutdown") {
                        Console.WriteLine("Shutdown requested");
                        runServer = false;
                    } else {
                        var inputStream = req.InputStream;
                        System.IO.StreamReader reader = new System.IO.StreamReader(inputStream, Encoding.UTF8);
                        string requestContent = await reader.ReadToEndAsync();
                        var syncRequest = jsonMapper.Read<SyncRequest>(requestContent);
                        var syncResponse = Execute(syncRequest);
                        var jsonResponse = jsonMapper.Write(syncResponse);
                        
                        // Write the response info
                        byte[] data = Encoding.UTF8.GetBytes(jsonResponse);
                        int len = data.Length;

                        resp.ContentType = "application/json";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = len;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, len);
                        resp.Close();
                    }
                }
            }
        }
        
        public void Run()
        {
            // Create a Http server and start listening for incoming connections
            listener.Start();
            Log($"Listening for connections on {this.endpoint}");

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }

        private void Log(string msg) {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }
    }
    
    public class HostContainer : EntityContainer
    {
        private readonly EntityContainer local;

        public HostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override void CreateEntities(ICollection<KeyValue> entities) {
            local.CreateEntities(entities);
        }

        public override void UpdateEntities(ICollection<KeyValue> entities) {
            local.UpdateEntities(entities);
        }

        public override ICollection<KeyValue> ReadEntities(ICollection<string> ids) {
            var result = local.ReadEntities(ids);
            return result;
        }
    }
}
