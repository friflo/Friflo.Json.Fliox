// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IServer
    {
        void    Start();
        void    Run();
        Task    RunAsync();
        void    Stop();
    }
}