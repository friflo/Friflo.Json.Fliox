// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public partial class ClusterStore : FlioxClient
    {
        public  readonly    EntitySet <string, CatalogDatabase> catalogs;
        public  readonly    EntitySet <string, CatalogSchema>   schemas;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    public class CatalogDatabase {
        [Fri.Required]  public  string                          id;
        [Fri.Required]  public  string                          databaseType;
        [Fri.Required]  public  string[]                        containers;
        [Fri.Required]  public  string[]                        commands;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class CatalogSchema {
        [Fri.Required]  public  string                          id;
        [Fri.Required]  public  string                          schemaName;
        [Fri.Required]  public  string                          schemaPath;
        [Fri.Required]  public  Dictionary<string,JsonValue>    jsonSchemas;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // --- commands
    public class CatalogList {
        [Fri.Required]  public  List<CatalogDatabase>           catalogs;
    }
}