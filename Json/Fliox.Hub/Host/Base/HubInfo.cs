// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace

using System;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// Contains general information about a Hub describing the development environment. <br/>
    /// Clients can request this information with the command <b>std.Details</b>
    /// </summary>
    public sealed class HubInfo {
        // ReSharper disable once InconsistentNaming
        /// <summary>host name used to identify a specific host in a network. Not null. Default: 'host'</summary>
        public  string  hostName { get => name; set => name = value ?? throw new ArgumentNullException(nameof(hostName)); }
        /// <summary>project name</summary>
        public  string  projectName;
        /// <summary>project website url</summary>
        public  string  projectWebsite;
        /// <summary>environment name. E.g. dev, tst, stg, prd</summary>
        public  string  envName;
        
        private string  name = "host";
        /// <summary>
        /// the color used to display the environment name in GUI's using CSS color format.<br/>
        /// E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
        /// </summary>
        public  string  envColor;

        public override string ToString() {
            var env = envName != null ? $" · {envName}" : "";
            return $"{projectName ?? ""}{env}";
        }
    }
}