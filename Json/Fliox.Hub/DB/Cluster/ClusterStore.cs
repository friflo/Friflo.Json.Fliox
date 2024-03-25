// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

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
        
        public CommandTask<List<ModelFiles>> ModelFiles(ModelFilesQuery value) => send.Command<ModelFilesQuery, List<ModelFiles>>(value);
        
        public ClusterStore (FlioxHub hub, string dbName = null) : base(hub, dbName) { }
    }
}
