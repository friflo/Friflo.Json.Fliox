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
    /// <see cref="ClusterStore"/> provide information about databases hosted by the Hub: <br/>
    /// - available containers aka tables per database <br/>
    /// - available commands per database <br/>
    /// - the schema assigned to each database
    /// </summary>
    public partial class ClusterStore : FlioxClient
    {
        // --- containers
        public  readonly    EntitySet <string, DbContainers>    containers;
        public  readonly    EntitySet <string, DbMessages>      messages;
        public  readonly    EntitySet <string, DbSchema>        schemas;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    /// <summary><see cref="containers"/> and <see cref="storage"/> type of a database</summary>
    public class DbContainers {
        /// <summary>database name</summary>
        [Req]   public  string                          id;
        /// <summary><see cref="storage"/> type. e.g. memory, file-system, ...</summary>
        [Req]   public  string                          storage;
        /// <summary>collection of database <see cref="containers"/> </summary>
        [Req]   public  string[]                        containers;
                        
        public override string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary><see cref="commands"/> and <see cref="messages"/> of a database</summary>
    public class DbMessages {
        /// <summary>database name</summary>
        [Req]   public  string                          id;
        /// <summary>list of <see cref="commands"/> exposed by the database</summary>
        [Req]   public  string[]                        commands;
        /// <summary>list of <see cref="messages"/> exposed by the database</summary>
        [Req]   public  string[]                        messages;
                        
        public override string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>schema assigned to a database</summary>
    public class DbSchema {
        /// <summary>database name</summary>
        [Req]   public  string                          id;
        /// <summary>refer a type definition of the JSON Schema referenced with <see cref="schemaPath"/></summary>
        [Req]   public  string                          schemaName;
        /// <summary>refer a JSON Schema in <see cref="jsonSchemas"/></summary>
        [Req]   public  string                          schemaPath;
        /// <summary>map of JSON Schemas. Each JSON Schema is identified by its unique path</summary>
        [Req]   public  Dictionary<string, JsonValue>   jsonSchemas;
                        
        public override         string  ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // --- commands
    /// <summary>list of container statistics. E.g. entity <see cref="ContainerStats.count"/> per container</summary>
    public class DbStats {
                public  ContainerStats[]                containers;
    }
    
    /// <summary>statistics of a single container. E.g. entity <see cref="ContainerStats.count"/></summary>
    public class ContainerStats {
        /// <summary>container <see cref="name"/></summary>
        [Req]   public  string                          name;
        /// <summary><see cref="count"/> of entities / records</summary>
                public  long                            count;
            
        public override string                          ToString() => $"{name} - count: {count}";
    }
    
    /// <summary>general information about a Hub</summary>
    public class HostDetails {
        [Req]   public  string                          version;
                public  string                          hostName;
                /// <summary>project name</summary>
                public  string                          projectName;
                /// <summary>link to a website describing project and Hub</summary>
                public  string                          projectWebsite;
                /// <summary>environment name. e.g. 'dev', 'test', 'staging', 'prod'</summary>
                public  string                          envName;
                /// <summary>
                /// the color used to display the environment name in GUI's using CSS color format.<br/>
                /// E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
                /// </summary>
                public  string                          envColor;
    }
    
    /// <summary>list of all databases of a Hub</summary>
    public class HostCluster {
        [Req]   public  List<DbContainers>              databases;
    }
}