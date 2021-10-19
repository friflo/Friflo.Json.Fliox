// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Microsoft.AspNetCore.Http;

namespace Friflo.Json.Fliox.DB.AspNetCore
{
    public class AspNetCoreHostHostDatabase : RemoteHostDatabase
    {
        public AspNetCoreHostHostDatabase(EntityDatabase local, DbOpt opt = null) : base(local, opt) {
        }
        
        public async Task ExecuteGet (HttpContext context) {
            await context.Response.WriteAsync("Hello Fliox!");
        }
        
        public async Task ExecutePost (HttpContext context) {
            await context.Response.WriteAsync("Hello World!");
        }

    }
}