// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Web;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Remote.Rest.RestRequestType;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

namespace Friflo.Json.Fliox.Hub.Remote.Rest
{
    internal sealed class RestHandler
    {
        private const   string      RestBase = "/rest";
        
        public          string[]    Routes => new []{ RestBase };

        public override string      ToString() => RestBase;

        internal static bool IsMatch(RequestContext context) {
            return RequestContext.IsBasePath(RestBase, context.route);
        }
        
        internal static RestRequest GetRestRequest(RequestContext context, in JsonValue body)
        {
            var route = context.route;
            if (route.Length == RestBase.Length) {
                // --------------    GET            /rest
                if (context.method == "GET") { 
                    return new RestRequest(command, "cluster", Std.HostCluster, new JsonValue());
                }
                return new RestRequest("invalid request", "access to root only applicable with GET", 400);
            }
            var pool            = context.Pool;
            var method          = context.method;
            var queryParams     = HttpUtility.ParseQueryString(context.query);
            var commandName     = queryParams["cmd"];
            var messageName     = queryParams["msg"];
            var isGet           = method == "GET";
            var isPost          = method == "POST";
            var resourcePath    = route.Substring(RestBase.Length + 1);
            var res             = new Resource(resourcePath);
            
            // ------------------    GET            /rest/database?cmd=...   /rest/database?msg=...
            //                       POST           /rest/database?cmd=...   /rest/database?msg=...
            if ((commandName != null || messageName != null) && (isGet || isPost)) {
                if (res.length != 1) {
                    return new RestRequest(RestUtils.GetErrorType(commandName), $"messages & commands operate on database. was: {resourcePath}", 400);
                }
                var database = res.database;
                if (database == context.hub.database.name)
                    database = null;
                JsonValue param;
                if (isPost) {
                    param = body;
                } else {
                    var queryValue = queryParams["param"];
                    param = new JsonValue(queryValue);
                }
                // Treat missing request body as null
                if (param.Count == 0) {
                    param = new JsonValue();
                } else {
                    if (!RestUtils.IsValidJson(pool, param, out string error)) {
                        return new RestRequest(RestUtils.GetErrorType(commandName), $"invalid param - {error}", 400);
                    }
                }
                if (commandName != null) {
                    return new RestRequest(command, database, commandName, param);
                }
                return new RestRequest(message, database, messageName, param);
            }

            if (res.error != null) {
                return new RestRequest("invalid path /database/container/id", res.error, 400);
            }
            
            // ------------------    POST           /rest/database/container/bulk-get
            //                       POST           /rest/database/container/bulk-delete
            if (isPost && res.length == 3) {
                bool getEntities    = false;
                var bulk            = res.id;
                switch (bulk) {
                    case "bulk-get":
                        getEntities = true;
                        break;
                    case "bulk-delete":
                        break;
                    default:
                        return new RestRequest($"post failed", $"invalid container operation: {bulk}", 400);
                }
                JsonKey[] keys;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    keys        = reader.Read<JsonKey[]>(body);
                    if (reader.Error.ErrSet) {
                        return new RestRequest("invalid id list", reader.Error.ToString(), 400);
                    }
                }
                if (getEntities) {
                    return new RestRequest(read, res.database, res.container, keys);
                }
                return new RestRequest(delete, res.database, res.container, keys);
            }
            
            if (isGet) {
                // --------------    GET            /rest/database
                if (res.length == 1) {
                    return new RestRequest(command, res.database, Std.Containers, new JsonValue());
                }
                // --------------    GET            /rest/database/container
                if (res.length == 2) {
                    var idsParam = queryParams["ids"];
                    if (idsParam != null) {
                        var ids     = idsParam == "" ? Array.Empty<string>() : idsParam.Split(',');
                        var keys    = RestUtils.GetKeysFromIds(ids);
                        return new RestRequest(read, res.database, res.container, keys);
                    }
                    return new RestRequest(query, res.database, res.container, null, default, queryParams);
                }
                // --------------    GET            /rest/database/container/id
                if (res.length == 3) {
                    return new RestRequest(readOne, res.database, res.container, res.id, default, null);
                }
                return new RestRequest("invalid request", "expect: /database/container/id", 400);
            }
            
            var isDelete = method == "DELETE";
            if (isDelete) {
                // --------------    DELETE         /rest/database/container/id
                if (res.length == 3) {
                    var keys = new [] { new JsonKey(res.id) };
                    return new RestRequest(delete, res.database, res.container, keys);
                }
                return new RestRequest("invalid request", "expect: /database/container/id", 400);
            }
            // ------------------    PUT            /rest/database/container        ?create
            //                       PUT            /rest/database/container/id     ?create
            if (method == "PUT") {
                if (res.length != 2 && res.length != 3) {
                    return new RestRequest("invalid PUT", "expect: /database/container or /database/container/id", 400);
                }
                if (!RestUtils.IsValidJson(pool, body, out string error)) {
                    return new RestRequest("PUT failed", error, 400);
                }
                var resource2   = res.length == 3 ? res.id : null;
                return new RestRequest(write, res.database, res.container, resource2, body, queryParams);
            }
            // ------------------    PATCH          /rest/database/container/id
            if (method == "PATCH") {
                if (res.length != 2 && res.length != 3) {
                    return new RestRequest("invalid PATCH", "expect: /database/container or /database/container/id", 400);
                }
                if (!RestUtils.IsValidJson(pool, body, out string error)) {
                    return new RestRequest("PATCH failed", error, 400);
                }
                var resource2   = res.length == 3 ? res.id : null; // id
                return new RestRequest(merge, res.database, res.container, resource2, body, queryParams);
            }
            return new RestRequest("invalid path/method", route, 400);
        }
    }


    internal readonly struct RestResult {
        internal  readonly  SyncResponse    syncResponse;
        internal  readonly  SyncTaskResult  taskResult;
        
        internal RestResult (SyncResponse syncResponse, SyncTaskResult  taskResult) {
            this.syncResponse   = syncResponse;
            this.taskResult     = taskResult;
        }
    } 

    internal readonly struct Resource {
        internal  readonly  string  database;
        internal  readonly  string  container;
        internal  readonly  string  id;

        internal  readonly  int     length;
        internal  readonly  string  error;
        
        private   readonly  string  path;

        public    override  string  ToString() => error == null ? path : $"{path} error: {error}";

        internal Resource (string resourcePath) {
            path            = resourcePath; 
            var resources   = resourcePath.Split('/');
            length          = resources.Length;
            if (resources[length - 1] == "")
                length--;
            database    = length > 0 ? resources[0] : null;
            container   = length > 1 ? resources[1] : null;
            id          = length > 2 ? resources[2] : null;
            if (database == "") { 
                error = "empty database path";
                return;
            }
            if (container == "") { 
                error = "empty container path";
                return;
            }
            if (id == "") { 
                error = "empty id path";
            }
            error = null;
        }
    }
}