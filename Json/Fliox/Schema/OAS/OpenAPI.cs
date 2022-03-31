// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Schema.OAS
{
    public sealed class OpenAPI {
        public  string              version;
        public  OpenAPIInfo         info;
        public  List<OpenAPIServer> servers;
    }
    
    public sealed class OpenAPIInfo {
        public  OpenAPIContact      contact;
        public  OpenAPILicense      license;
    }
    
    public sealed class OpenAPIContact {
        public  string              name;
        public  string              url;
        public  string              email;
    }
    
    public sealed class OpenAPILicense {
        public  string              name;
        public  string              url;
    }

    public sealed class OpenAPIServer {
        public  string              url;
        public  string              description;
    }
    

}