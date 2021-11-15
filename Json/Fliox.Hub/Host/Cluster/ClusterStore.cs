// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Cluster
{
    public partial class ClusterStore : FlioxClient
    {
        public  readonly    EntitySet <string, Catalog>       catalogs;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    
    public class Catalog {
        [Fri.Key]       public  string          name;
        
        [Fri.Required]  public  List<string>    containers;
                        
        public override         string          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
}