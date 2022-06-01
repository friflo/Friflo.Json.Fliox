// Copyright (c) Ullrich Praetz. All rights reserved.
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
                if (attr.AttributeType == typeof(OpenAPIServer)) {
                    var namedArguments = attr.NamedArguments;
                    if (namedArguments != null) {
                        var server = GetOpenAPIServerAttributes(namedArguments);
                        if (servers == null)
                            servers = new List<SchemaInfoServer>();
                        servers.Add(server);
                    }
                }
            }
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(OpenAPI)) {
                    var namedArguments = attr.NamedArguments;
                    if (namedArguments != null) {
                        return GetOpenAPIAttributes(namedArguments, servers);
                    }
                }

            }
            return null;
        }
        
        private static SchemaInfo GetOpenAPIAttributes(IList<CustomAttributeNamedArgument> namedArguments, List<SchemaInfoServer> servers) {
            string  version         = null;
            string  termsOfService  = null;
            string  licenseName     = null;
            string  licenseUrl      = null;
            string  contactName     = null;
            string  contactUrl      = null;
            string  contactEmail    = null;
            foreach (var args in  namedArguments) {
                var value = (string)args.TypedValue.Value;
                switch (args.MemberName) {
                    case nameof(OpenAPI.Version):       version       = value;    break;
                    case nameof(OpenAPI.TermsOfService):termsOfService= value;    break;
                    case nameof(OpenAPI.LicenseName):   licenseName   = value;    break;
                    case nameof(OpenAPI.LicenseUrl):    licenseUrl    = value;    break;
                    case nameof(OpenAPI.ContactName):   contactName   = value;    break;
                    case nameof(OpenAPI.ContactUrl):    contactUrl    = value;    break;
                    case nameof(OpenAPI.ContactEmail):  contactEmail  = value;    break;
                }
            }
            return new SchemaInfo(version, termsOfService, contactName, contactUrl, contactEmail, licenseName, licenseUrl, servers);
        }
        
        private static SchemaInfoServer GetOpenAPIServerAttributes(IList<CustomAttributeNamedArgument> namedArguments) {
            string description  = null;
            string url          = null;
            foreach (var args in namedArguments) {
                var value = (string)args.TypedValue.Value;
                switch (args.MemberName) {
                    case nameof(OpenAPIServer.Description): description = value;    break;
                    case nameof(OpenAPIServer.Url):         url         = value;    break;
                }
            }
            return new SchemaInfoServer(url, description);
        }
    }
}