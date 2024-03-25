// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Json.Fliox.Schema.OAS;

// ReSharper disable PossibleMultipleEnumeration
namespace Friflo.Json.Fliox.Schema.Definition
{
    public sealed class SchemaInfoServer {
        public  readonly string  url;
        public  readonly string  description;
        
        public SchemaInfoServer (string  url, string  description) {
            this.url            = url;
            this.description    = description;
        }
    }

    /// <summary>
    /// The <see cref="SchemaInfo"/> contains meta data about a schema compatible to the fields <b>info</b> and <b>servers</b>
    /// in the <a href="https://spec.openapis.org/oas/v3.0.0#openapi-object">OpenAPI Specification 3.0.0</a>
    /// </summary>
    public sealed class SchemaInfo
    {
        public  readonly    string  version;
        public  readonly    string  termsOfService;
        
        public  readonly    string  contactName;
        public  readonly    string  contactUrl;
        public  readonly    string  contactEmail;
        
        public  readonly    string  licenseName;
        public  readonly    string  licenseUrl;
        
        public  readonly IReadOnlyList<SchemaInfoServer>    servers;

        /// <summary>Will be used for <see cref="JSON.JSONSchema"/></summary>
        public SchemaInfo(OpenApi openApi) {
            if (openApi == null)
                return;
            version         = openApi.version;
            termsOfService  = openApi.termsOfService;
            
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
        
        private SchemaInfo(
            string      version,
            string      termsOfService,
            string      contactName,
            string      contactUrl,
            string      contactEmail,
            string      licenseName,
            string      licenseUrl,
            ICollection<SchemaInfoServer>   servers)
        {
            this.version        = version;
            this.termsOfService = termsOfService;
            this.contactName    = contactName;
            this.contactUrl     = contactUrl;
            this.contactEmail   = contactEmail;
            this.licenseName    = licenseName;
            this.licenseUrl     = licenseUrl;
            this.servers        = servers.ToList();
        }
        
        public static SchemaInfo GetSchemaInfo(Type schemaType)
        {
            var                     attributes  = schemaType.CustomAttributes;
            List<SchemaInfoServer>  servers     = null;
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(OpenAPIServerAttribute)) {
                    var arguments   = attr.ConstructorArguments;
                    var server      = GetOpenAPIServerAttributes(arguments);
                    if (servers == null)
                        servers = new List<SchemaInfoServer>();
                    servers.Add(server);
                }
            }
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(OpenAPIAttribute)) {
                    var arguments   = attr.ConstructorArguments;
                    return GetOpenAPIAttributes(arguments, servers);
                }

            }
            return null;
        }
        
        private static string GetArg (IList<CustomAttributeTypedArgument> arguments, int index) {
            return arguments.Count < index + 1 ? null : (string)arguments[index].Value;
        }
        
        private static SchemaInfo GetOpenAPIAttributes(IList<CustomAttributeTypedArgument> arguments, List<SchemaInfoServer> servers) {
            string  version         = GetArg(arguments, 0);
            string  termsOfService  = GetArg(arguments, 1);
            string  licenseName     = GetArg(arguments, 2);
            string  licenseUrl      = GetArg(arguments, 3);
            string  contactName     = GetArg(arguments, 4);
            string  contactUrl      = GetArg(arguments, 5);
            string  contactEmail    = GetArg(arguments, 6);
            return new SchemaInfo(version, termsOfService, contactName, contactUrl, contactEmail, licenseName, licenseUrl, servers);
        }
        
        private static SchemaInfoServer GetOpenAPIServerAttributes(IList<CustomAttributeTypedArgument> arguments) {
            string url          = GetArg(arguments, 0);
            string description  = GetArg(arguments, 1);
            return new SchemaInfoServer(url, description);
        }
    }
}