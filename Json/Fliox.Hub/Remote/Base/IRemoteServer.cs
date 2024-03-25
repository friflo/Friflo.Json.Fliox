// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IRemoteServer
    {
        void    Start();
        /// <summary>
        /// Implementations should use<br/>
        /// <c>.GetAwaiter().GetResult();</c><br/>
        /// instead of<br/>
        /// <c>.Wait()</c><br/>
        /// to get a more useful stacktrace in case of exceptions. See<br/>
        /// https://stackoverflow.com/questions/36426937/what-is-the-difference-between-wait-vs-getawaiter-getresult
        /// </summary>
        void    Run();
        Task    RunAsync();
        void    Stop();
    }
}