// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.WebRTC.DB
{
    // ---------------------------------- entity models ----------------------------------
    public sealed class WebRtcPeer {
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

}