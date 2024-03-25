// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable UnassignedField.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Schema.OAS
{
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#openapi-object">OpenAPI Object specification</a>
    /// </summary>
    public sealed class OpenApi {
        public  string              version;
        public  string              termsOfService;
        public  OpenApiInfo         info;
        public  List<OpenApiServer> servers;
    }
    
    public sealed class OpenApiInfo {
        public  OpenApiContact      contact;
        public  OpenApiLicense      license;
    }
    
    public sealed class OpenApiContact {
        public  string              name;
        public  string              url;
        public  string              email;
    }
    
    public sealed class OpenApiLicense {
        public  string              name;
        public  string              url;
    }

    public sealed class OpenApiServer {
        public  string              url;
        public  string              description;
    }
}