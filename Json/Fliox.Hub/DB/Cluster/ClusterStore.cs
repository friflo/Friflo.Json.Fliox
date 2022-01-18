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
        public  readonly    EntitySet <string, DbContainers>    containers;
        public  readonly    EntitySet <string, DbSchema>        schemas;
        public  readonly    EntitySet <string, DbCommands>      commands;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    public class DbContainers {
        [Fri.Required]  public  string                          id;
        [Fri.Required]  public  string                          databaseType;
        [Fri.Required]  public  string[]                        containers;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    public class DbCommands {
        [Fri.Required]  public  string                          id;
        [Fri.Required]  public  string[]                        commands;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class DbSchema {
        [Fri.Required]  public  string                          id;
        [Fri.Required]  public  string                          schemaName;
        [Fri.Required]  public  string                          schemaPath;
        [Fri.Required]  public  Dictionary<string,JsonValue>    jsonSchemas;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // --- commands
    public class DbHubInfo {
        [Fri.Required]  public  string                          version;
                        public  string                          hostName;
                        public  string                          description;
                        public  string                          website;
    }
    
    public class DbHubCluster {
        [Fri.Required]  public  List<DbContainers>              databases;
    }
}