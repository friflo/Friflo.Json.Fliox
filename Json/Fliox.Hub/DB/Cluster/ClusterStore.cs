// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /// <summary>
    /// Provide information about databases exposed by the Hub. These are: <br/>
    /// - available containers aka tables per database <br/>
    /// - available commands per database <br/>
    /// - the schema assigned to each database
    /// </summary>
    public partial class ClusterStore : FlioxClient
    {
        // --- containers
        public  readonly    EntitySet <string, DbContainers>    containers;
        public  readonly    EntitySet <string, DbCommands>      commands;
        public  readonly    EntitySet <string, DbSchema>        schemas;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    public class DbContainers {
        [Req]   public  string                          id;
        [Req]   public  string                          databaseType;
        [Req]   public  string[]                        containers;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    public class DbCommands {
        [Req]   public  string                          id;
        [Req]   public  string[]                        commands;
                        
        public override string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class DbSchema {
        [Req]   public  string                          id;
        [Req]   public  string                          schemaName;
        [Req]   public  string                          schemaPath;
        [Req]   public  Dictionary<string,JsonValue>    jsonSchemas;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // --- commands
    public class DbStats {
                public  ContainerStats[]                containers;
    }
    
    public class ContainerStats {
        [Req]   public  string                          name;
                public  long                            count;
            
        public override string                          ToString() => $"{name} - count: {count}";
    }
    
    
    public class HostDetails {
        [Req]   public  string                          version;
                public  string                          hostName;
                public  string                          projectName;
                public  string                          projectWebsite;
                public  string                          envName;
                public  string                          envColor;
    }
    
    public class HostCluster {
        [Req]   public  List<DbContainers>              databases;
    }
}