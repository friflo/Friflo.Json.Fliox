// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        /// <summary>true if the database is the default database of a Hub</summary>
                    public  bool?                           defaultDB;
                        
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
    
    /// <summary>return the execution result performed by a transaction.</summary>
    public sealed class TransactionResult {
        /// <summary>
        /// The execution performed by the transaction.<br/>
        /// In case any task in the transaction failed the transaction performs a <see cref="TransactionCommand.Rollback"/>
        /// </summary>
        public TransactionCommand executed;
    }

    public enum TransactionCommand {
        Commit,
        Rollback,
    }
    
    public sealed class HostParam  {
                    public bool?                memory;
                    public bool?                gcCollect;
    }
    
    /// <summary>general information about a Hub</summary>
    public sealed class HostInfo {
        /// <summary>host name used to identify a specific host in a network.</summary>
        [Required]  public  string              hostName;       // not null
        /// <summary>host version</summary>
        [Required]  public  string              hostVersion;    // not null
        /// <summary>Fliox library version</summary>
        [Required]  public  string              flioxVersion;   // not null
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
        
                    public  HostMemory          memory;
    }
    
    public sealed class HostMemory {
        public  long            totalAllocatedBytes;
        public  long            totalMemory;
        public  HostGCMemory    gc;
    }
    
    /// <summary> See <a href="https://learn.microsoft.com/en-us/dotnet/api/system.gcmemoryinfo">GCMemoryInfo</a></summary>
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
                    public  List<string>        addGroups;
                    public  List<string>        removeGroups;
    }

    public sealed class UserResult {
        [Required]  public  List<string>        roles;
        [Required]  public  List<string>        groups;
        [Required]  public  List<string>        clients;
        /// <summary>number executed requests and tasks per database</summary>
        [Required]  public  List<RequestCount>  counts = new List<RequestCount>();
    }
    
    public sealed class ClientParam {
        /// <summary>Return the client id set in <see cref="Protocol.SyncRequest"/> or creates a new one in case is was not set.</summary>
                    public  bool?           ensureClientId;
        /// <summary>
        /// If <b>false</b> the hub try to send events to a client when the events are emitted.
        /// Sending events to a disconnected client will never arrive. <br/>
        /// If <b>true</b> the hub will store all unacknowledged events for a client in a FIFO queue and send them on reconnects.  
        /// </summary>
                    public  bool?           queueEvents;
    }
    
    public sealed class ClientResult {
        /// <summary> returns true if the host queue events for the client in case of disconnects </summary>
                    public  bool                queueEvents;
        /// <summary>
        /// return number of queued events not acknowledged by the client.
        /// Events are queued only if the client instruct the Hub to queue events by setting <see cref="ClientParam.queueEvents"/> = true 
        /// </summary>
                    public  int                 queuedEvents;
        /// <summary>return the client id set in the <see cref="Protocol.SyncRequest"/>. Can be null.<br/>
        /// A new client id is created in case any task requires a client id and the <see cref="Protocol.SyncRequest"/> did not set a client id.<br/>
        /// E.g. <see cref="ClientParam.ensureClientId"/> = true or <see cref="ClientParam.queueEvents"/> = true </summary>
                    public  string              clientId;
        /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
                    public  SubscriptionEvents? subscriptionEvents;
    }
    
    /// <summary>number of sent or queued client events and its message and change subscriptions</summary>
    public struct SubscriptionEvents {
        /// <summary>number of events sent to a client</summary>
        public  int                             seq;
        /// <summary>number of queued events not acknowledged by a client</summary>
        public  int                             queued;
        /// <summary>true if client is instructed to queue events for reliable event delivery in case of reconnects</summary>
        public  bool                            queueEvents;
        /// <summary>true if client is connected. Non remote client are always connected</summary>
        public  bool                            connected;
        /// <summary>
        /// The endpoint of the client events are sent to.<br/>
        /// E.g. <c>ws:[::1]:52089</c> for WebSockets, <c>udp:127.0.0.1:60005</c> for UDP or <c>in-process</c>
        /// </summary>
        public  string                          endpoint;
        /// <summary>message / command subscriptions of a client</summary>
        public  List<string>                    messageSubs;
        /// <summary>change subscriptions of a client</summary>
        public  List<ChangeSubscription>        changeSubs;
    }
    
    /// <summary>change subscription for a specific container</summary>
    public sealed class ChangeSubscription
    {
        /// <summary>name of subscribed container</summary>
        [Required]  public  string              container;
        /// <summary>type of subscribed changes like create, upsert, delete and patch</summary>
        [Required]  public  List<ChangeType>    changes;
        /// <summary>filter to narrow the amount of change events</summary>
                    public  string              filter;
    }
    
    /// <summary>number of requests and tasks executed per database</summary>
    public struct RequestCount {
        /// <summary>database name</summary>
        public              ShortString db;
        /// <summary>number of executed requests</summary>
        public              int         requests;
        /// <summary>number of executed tasks</summary>
        public              int         tasks;

        public override     string      ToString() => $"db: {db}, requests: {requests}, tasks: {tasks}";
    }
    

    /// <summary>Filter type used to specify the type of an entity change</summary>
    // ReSharper disable InconsistentNaming
    [Flags]
    public enum ChangeType
    {
        /// <summary>filter change events of created entities.</summary>
        create  = 1,
        /// <summary>filter change events of upserted entities.</summary>
        upsert  = 2,
        /// <summary>filter change events of entity patches.</summary>
        merge   = 4,
        /// <summary>filter change events of deleted entities.</summary>
        delete  = 8,
    }
    
    public sealed class ModelFilesQuery {
        /// <summary>specific database or null to retrieve all available models</summary>
                    public          string              db;
        /// <summary>specific model type - e.g. 'typescript' or null to retrieve all available model types</summary>
                    public          string              type;
    }

    public sealed class ModelFiles {
        [Required]  public          string              db;
        [Required]  public          string              type;
        [Required]  public          string              label;
        [Required]  public          List<ModelFile>     files;
        
                    public override string              ToString() => $"db: {db}, type: {type}";
    }
    
    public sealed class ModelFile {
        [Required]  public          string              path;
        [Required]  public          string              content;
        
                    public override string              ToString() => path;
    }
}