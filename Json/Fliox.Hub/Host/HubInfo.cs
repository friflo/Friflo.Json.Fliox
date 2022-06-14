// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// Contains general information about a Hub describing the development environment. <br/>
    /// Clients can request this information with the command <b>std.Details</b>
    /// </summary>
    public sealed class HubInfo {
        /// project name
        public  string  projectName;
        /// project website url
        public  string  projectWebsite;
        /// environment name. E.g. dev, tst, stg, prd
        public  string  envName;
        /// the color used to display the environment name in GUI's using CSS color format.<br/>
        /// E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
        public  string  envColor;

        public override string ToString() {
            var env = envName != null ? $" · {envName}" : "";
            return $"{projectName ?? ""}{env}";
        }
    }
}