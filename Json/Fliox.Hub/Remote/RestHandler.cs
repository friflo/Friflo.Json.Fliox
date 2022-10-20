// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Web;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote
{
    internal static partial class Rest {
        
        internal sealed class RestHandler : IRequestHandler
        {
            private const   string      RestBase = "/rest";
            
            public          string[]    Routes => new []{ RestBase };
            
            public bool IsMatch(RequestContext context) {
                return RequestContext.IsBasePath(RestBase, context.route);
            }
                
            public async Task HandleRequest(RequestContext context) {
                var route = context.route;
                if (route.Length == RestBase.Length) {
                    // --------------    GET            /rest
                    if (context.method == "GET") { 
                        await Command(context, "cluster", Std.HostCluster, new JsonValue()).ConfigureAwait(false); 
                        return;
                    }
                    context.WriteError("invalid request", "access to root only applicable with GET", 400);
                    return;
                }
                var pool            = context.Pool;
                var method          = context.method;
                var queryParams     = HttpUtility.ParseQueryString(context.query);
                var command         = queryParams["command"];
                var message         = queryParams["message"];
                var isGet           = method == "GET";
                var isPost          = method == "POST";
                var resourcePath    = route.Substring(RestBase.Length + 1);
                var res             = new Resource(resourcePath);
                
                // ------------------    GET            /rest/database?command=...   /database?message=...
                //                       POST           /rest/database?command=...   /database?message=...
                if ((command != null || message != null) && (isGet || isPost)) {
                    if (res.length != 1) {
                        context.WriteError(GetErrorType(command), $"messages & commands operate on database. was: {resourcePath}", 400);
                        return;
                    }
                    var database = res.database;
                    if (database == context.hub.DatabaseName)
                        database = null;
                    JsonValue param;
                    if (isPost) {
                        param = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                    } else {
                        var queryValue = queryParams["param"];
                        param = new JsonValue(queryValue);
                    }
                    // Treat missing request body as null
                    if (param.Length == 0) {
                        param = new JsonValue();
                    } else {
                        if (!IsValidJson(pool, param, out string error)) {
                            context.WriteError(GetErrorType(command), $"invalid param - {error}", 400);
                            return;
                        }
                    }
                    if (command != null) {
                        await Command(context, database, command, param).ConfigureAwait(false); 
                        return;
                    }
                    await Message(context, database, message, param).ConfigureAwait(false);
                    return;
                }

                if (res.error != null) {
                    context.WriteError("invalid path /database/container/id", res.error, 400);
                    return;
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
                            context.WriteError($"post failed", $"invalid container operation: {bulk}", 400);
                            return;
                    }
                    JsonKey[] keys;
                    using (var pooled = pool.ObjectMapper.Get()) {
                        var reader  = pooled.instance.reader;
                        var body    = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                        keys        = reader.Read<JsonKey[]>(body);
                        if (reader.Error.ErrSet) {
                            context.WriteError("invalid id list", reader.Error.ToString(), 400);
                            return;
                        }
                    }
                    if (getEntities) {
                        await GetEntitiesById (context, res.database, res.container, keys).ConfigureAwait(false);
                        return;
                    }
                    await DeleteEntities(context, res.database, res.container, keys).ConfigureAwait(false);
                    return;
                }
                
                if (isGet) {
                    // --------------    GET            /rest/database
                    if (res.length == 1) {
                        await Command(context, res.database, Std.Containers, new JsonValue()).ConfigureAwait(false); 
                        return;
                    }
                    // --------------    GET            /rest/database/container
                    if (res.length == 2) {
                        var idsParam = queryParams["ids"];
                        if (idsParam != null) {
                            var ids     = idsParam == "" ? Array.Empty<string>() : idsParam.Split(',');
                            var keys    = GetKeysFromIds(ids);
                            await GetEntitiesById (context, res.database, res.container, keys).ConfigureAwait(false);
                            return;
                        }
                        await QueryEntities(context, res.database, res.container, queryParams).ConfigureAwait(false);
                        return;
                    }
                    // --------------    GET            /rest/database/container/id
                    if (res.length == 3) {
                        await GetEntity(context, res.database, res.container, res.id).ConfigureAwait(false);    
                        return;
                    }
                    context.WriteError("invalid request", "expect: /database/container/id", 400);
                    return;
                }
                
                var isDelete = method == "DELETE";
                if (isDelete) {
                    // --------------    DELETE         /rest/database/container/id
                    if (res.length == 3) {
                        var keys = new [] { new JsonKey(res.id) };
                        await DeleteEntities(context, res.database, res.container, keys).ConfigureAwait(false);
                        return;
                    }
                    context.WriteError("invalid request", "expect: /database/container/id", 400);
                    return;
                }
                // ------------------    PUT            /rest/database/container        ?create
                //                       PUT            /rest/database/container/id     ?create
                if (method == "PUT") {
                    if (res.length != 2 && res.length != 3) {
                        context.WriteError("invalid PUT", "expect: /database/container or /database/container/id", 400);
                        return;
                    }
                    var value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                    if (!IsValidJson(pool, value, out string error)) {
                        context.WriteError("PUT failed", error, 400);
                        return;
                    }
                    var keyName     = queryParams["keyName"];
                    var resource2   = res.length == 3 ? res.id : null;
                    if (!HasQueryKey(queryParams, "create", out bool create, out error)) {
                        context.WriteError("PUT failed", error, 400);
                        return;
                    }
                    var type        = create ? TaskType.create : TaskType.upsert;
                    await PutEntities(context, res.database, res.container, resource2, keyName, value, type).ConfigureAwait(false);
                    return;
                }
                // ------------------    PATCH          /rest/database/container/id
                if (method == "PATCH") {
                    if (res.length != 3) {
                        context.WriteError("invalid PATCH", "expect: /database/container/id", 400);
                        return;
                    }
                    var patch = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                    if (!IsValidJson(pool, patch, out string error)) {
                        context.WriteError("PATCH failed", error, 400);
                        return;
                    }
                    var keyName     = queryParams["keyName"];
                    await PatchEntity(context, res.database, res.container, res.id, keyName, patch).ConfigureAwait(false);
                    // await MergeEntity(context, res.database, res.container, res.id, keyName, patch).ConfigureAwait(false);
                    return;
                }
                context.WriteError("invalid path/method", route, 400);
            }
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