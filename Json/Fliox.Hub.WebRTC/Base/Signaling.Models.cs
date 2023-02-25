// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    // ---------------------------------- entity models ----------------------------------
    public sealed class WebRtcHost {
        /// <summary>WebRtc host id</summary>
        [Required]  public  string                          id;
                        
        public override     string                          ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    
    public sealed class RegisterHost {
        public string name;
    }
    
    public sealed class RegisterHostResult {

    }
}