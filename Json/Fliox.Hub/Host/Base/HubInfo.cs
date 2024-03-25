// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// Contains general information about a Hub describing the development environment. <br/>
    /// Clients can request this information with the command <b>std.Details</b>
    /// </summary>
    public sealed class HubInfo {
        /// <summary>project name</summary>
        public  string  projectName;
        /// <summary>environment name. E.g. dev, tst, stg, prd</summary>
        public  string  envName;
        /// <summary>project website url</summary>
        public  string  projectWebsite;
        /// <summary>
        /// the color used to display the environment name in GUI's using CSS color format.<br/>
        /// E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
        /// </summary>
        public  string  envColor;
        
        public void Set(string projectName, string envName = null, string projectWebsite = null, string envColor = null) {
            this.projectName    = projectName;
            this.envName        = envName;
            this.projectWebsite = projectWebsite;
            this.envColor       = envColor;
        }

        public override string ToString() {
            var env = envName != null ? $" · {envName}" : "";
            return $"{projectName ?? ""}{env}";
        }
    }
}