// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    [Flags]
    internal enum FrameFlags {
        Fin                 = 0x80,
        Rsv1                = 0x40,
        Rsv2                = 0x20,
        Rsv3                = 0x10,
        //
        Opcode              = 0x0f,
    }
    
    internal enum Opcode {
        ContinuationFrame   = 0x00,
        TextFrame           = 0x01,
        BinaryFrame         = 0x02,
        ConnectionClose     = 0x08,
        Ping                = 0x09,
        Pong                = 0x0a
    }

    [Flags]
    internal enum LenFlags {
        Mask                = 0x80
    }
}