// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    // ---------------------------------- entity models ----------------------------------
    /// <summary><see cref="containers"/> and <see cref="storage"/> type of a database</summary>
    public sealed class DbContainers {
        /// <summary>database name</summary>
        [Required]  public  string                          id;
        /// <summary><see cref="storage"/> type. e.g. memory, file-system, ...</summary>
        [Required]  public  string                          storage;
        /// <summary>list of database <see cref="containers"/></summary>
        [Required]  public  string[]                        containers;
                        
        public override     string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary><see cref="commands"/> and <see cref="messages"/> of a database</summary>
    public sealed class DbMessages {
        /// <summary>database name</summary>
        [Required]  public  string                          id;
        /// <summary>list of database <see cref="commands"/></summary>
        [Required]  public  string[]                        commands;
        /// <summary>list of database <see cref="messages"/></summary>
        [Required]  public  string[]                        messages;
                        
        public override     string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    /// <summary>
    /// A <see cref="DbSchema"/> can be assigned to a database to specify its <b>containers</b>, <b>commands</b> and <b>messages</b>.<br/>
    /// The types used by the Schema are declared within <see cref="jsonSchemas"/>.<br/>
    /// The type referenced by the tuple <see cref="schemaName"/> / <see cref="schemaPath"/> specify the
    /// database containers, commands and messages. 
    /// </summary>
    public sealed class DbSchema {
        /// <summary>database name</summary>
        [Required]  public  string                          id;
        /// <summary>refer a type definition of the JSON Schema referenced with <see cref="schemaPath"/></summary>
        [Required]  public  string                          schemaName;
        /// <summary>refer a JSON Schema in <see cref="jsonSchemas"/></summary>
        [Required]  public  string                          schemaPath;
        /// <summary>map of <b>JSON Schemas</b> each containing a set of type definitions.<br/>
        /// Each JSON Schema is identified by its unique path</summary>
        [Required]  public  Dictionary<string, JsonValue>   jsonSchemas;
                        
        public override     string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    // ---------------------------- command models - aka DTO's ---------------------------
    /// <summary>list of container statistics. E.g. the number of entities per container</summary>
    public sealed class DbStats {
        /// <summary>list of container statistics - number of entities per container</summary>
                    public  ContainerStats[]    containers;
    }
    
    /// <summary>statistics of a single container. E.g. the number of entities in a container</summary>
    public sealed class ContainerStats {
        /// <summary>container name</summary>
        [Required]  public  string              name;
        /// <summary>number of entities / records within a container</summary>
                    public  long                count;
            
        public override     string              ToString() => $"{name} - count: {count}";
    }
    
    public sealed class HostParam  {
                    public bool?                gcCollect;
    }
    
    /// <summary>general information about a Hub</summary>
    public sealed class HostInfo {
        /// <summary>host version</summary>
        [Required]  public  string              hostVersion;
        /// <summary>Fliox library version</summary>
        [Required]  public  string              flioxVersion;
        /// <summary>host name. Used as <see cref="DB.Monitor.HostHits.id"/> in
        /// <see cref="DB.Monitor.MonitorStore.hosts"/> of database <b>monitor</b></summary>
                    public  string              hostName;
        /// <summary>project name</summary>
                    public  string              projectName;
        /// <summary>link to a website describing project and Hub</summary>
                    public  string              projectWebsite;
        /// <summary>environment name. e.g. 'dev', 'test', 'staging', 'prod'</summary>
                    public  string              envName;
        /// <summary>
        /// the color used to display the environment name in GUI's using CSS color format.<br/>
        /// E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
        /// </summary>
                    public  string              envColor;
        /// <summary> is true if host support Pub-Sub.</summary>
        /// <remarks> if true <see cref="Host.FlioxHub.EventDispatcher"/> is assigned </remarks>
                    public  bool                pubSub;
        /// <summary>routes configures by <see cref="Remote.HttpHost"/> - commonly below <c>/fliox</c></summary>
        [Required]  public  List<string>        routes;
        
        [Required]  public  HostMemory          memory;
    }
    
    public sealed class HostMemory {
        public  long            totalAllocatedBytes;
        public  long            totalMemory;
        public  HostGCMemory    gc;
    }
    
    /// <summary> <see cref="System.GCMemoryInfo"/> </summary>
    public sealed class HostGCMemory {
        public  long            highMemoryLoadThresholdBytes;
        public  long            totalAvailableMemoryBytes;
        public  long            memoryLoadBytes;
        public  long            heapSizeBytes;
        public  long            fragmentedBytes;
    }
    
    /// <summary>All <see cref="databases"/> hosted by Hub</summary>
    public sealed class HostCluster {
        /// <summary>list of <see cref="databases"/> hosted by Hub</summary>
        [Required]  public  List<DbContainers>  databases;
    }
    
    public sealed class UserParam {
                    public  List<string>    addGroups;
                    public  List<string>    removeGroups;
    }

    public sealed class UserResult {
        [Required]  public  string[]        groups;
    }
    
    public sealed class ClientParam {
    }
    
    public sealed class ClientResult {
                    public  int             queuedEvents;
    }
}