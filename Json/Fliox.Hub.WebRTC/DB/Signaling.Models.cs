// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.DB.WebRTC
{
    // ---------------------------------- entity models ----------------------------------
    public sealed class WebRtcPeer {
        /// <summary>client id</summary>
        [Required]  public  string                          id;
                        
        public override     string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    public sealed class AddHost {
        public string name;
    }
    
    public sealed class AddHostResult {

    }
}