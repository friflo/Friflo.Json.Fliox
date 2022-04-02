// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.OAS;

namespace Friflo.Json.Fliox.Schema.Definition
{
    public class SchemaInfoServer {
        public  readonly string  url;
        public  readonly string  description;
        
        public SchemaInfoServer (string  url, string  description) {
            this.url            = url;
            this.description    = description;
        }
    }

    public class SchemaInfo
    {
        public  readonly    string  version;
        
        public  readonly    string  contactName;
        public  readonly    string  contactUrl;
        public  readonly    string  contactEmail;
        
        public  readonly    string  licenseName;
        public  readonly    string  licenseUrl;
        
        public  readonly IReadOnlyList<SchemaInfoServer>    servers;

        
        public SchemaInfo(OpenApi openApi) {
            if (openApi == null)
                return;
            version         = openApi.version;
            
            contactName     = openApi.info?.contact?.name;
            contactUrl      = openApi.info?.contact?.url;
            contactEmail    = openApi.info?.contact?.email;
            
            licenseName     = openApi.info?.license?.name;
            licenseUrl      = openApi.info?.license?.url;
            
            var infoServers = new List<SchemaInfoServer>();
            servers         = infoServers;
            if (openApi.servers != null) {
                foreach (var server in openApi.servers) {
                    infoServers.Add(new SchemaInfoServer (server.url, server.description));
                }
            }
        }
    }
}