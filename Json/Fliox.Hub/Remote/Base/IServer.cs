// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public interface IServer
    {
        void    Start();
        void    Run();
        void    Stop();
    }
}