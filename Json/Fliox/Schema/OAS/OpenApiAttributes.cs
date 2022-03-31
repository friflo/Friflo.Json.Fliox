// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Schema.OAS
{
    public static class OpenAPIAttributes
    {
        public static OpenApi GetOpenAPI(Type schemaType)
        {
            var     attributes  = schemaType.CustomAttributes;
            OpenApi openAPI     = null;
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.OpenAPI)) {
                    var namedArguments = attr.NamedArguments;
                    if (namedArguments != null) {
                        if (openAPI == null)
                            openAPI = CreateInstance();
                        GetOpenAPIAttributes(openAPI, namedArguments);
                    }
                }
                if (attr.AttributeType == typeof(Fri.OpenAPIServer)) {
                    var namedArguments = attr.NamedArguments;
                    if (namedArguments != null) {
                        if (openAPI == null)
                            openAPI = CreateInstance();
                        GetOpenAPIServerAttributes(openAPI, namedArguments);
                    }
                }
            }
            return openAPI;
        }
        
        private static OpenApi CreateInstance() {
            return new OpenApi {
                info = new OpenApiInfo {
                    contact = new OpenApiContact(),
                    license = new OpenApiLicense()
                },
                servers = new List<OpenApiServer>()
            };
        }
        
        private static void GetOpenAPIAttributes(OpenApi openAPI, IList<CustomAttributeNamedArgument> namedArguments) {
            foreach (var args in namedArguments) {
                var value = (string)args.TypedValue.Value;
                switch (args.MemberName) {
                    case nameof(Fri.OpenAPI.Version):       openAPI.version             = value;    break;
                    case nameof(Fri.OpenAPI.LicenseName):   openAPI.info.license.name   = value;    break;
                    case nameof(Fri.OpenAPI.LicenseUrl):    openAPI.info.license.url    = value;    break;
                    case nameof(Fri.OpenAPI.ContactName):   openAPI.info.contact.name   = value;    break;
                    case nameof(Fri.OpenAPI.ContactUrl):    openAPI.info.contact.url    = value;    break;
                    case nameof(Fri.OpenAPI.ContactEmail):  openAPI.info.contact.email  = value;    break;
                }
            }
        }
        
        private static void GetOpenAPIServerAttributes(OpenApi openAPI, IList<CustomAttributeNamedArgument> namedArguments) {
            var server = new OpenApiServer();
            openAPI.servers.Add(server);
            foreach (var args in namedArguments) {
                var value = (string)args.TypedValue.Value;
                switch (args.MemberName) {
                    case nameof(Fri.OpenAPIServer.Description): server.description  = value;    break;
                    case nameof(Fri.OpenAPIServer.Url):         server.url          = value;    break;
                }
            }
        }
    }
}