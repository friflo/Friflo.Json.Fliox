// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    internal enum Fin {
        More    = 0x00,
        Final   = 0x01
    }

    internal enum Rsv1 {
        Unset   = 0x00,
        Set     = 0x01
    }

    internal enum Rsv2 {
        Unset   = 0x00,
        Set     = 0x01
    }

    internal enum Rsv3 {
        Unset   = 0x00,
        Set     = 0x01
    }
    
    internal enum Opcode {
        ContinuationFrame   = 0x0,
        TextFrame           = 0x1,
        BinaryFrame         = 0x2,
        ConnectionClose     = 0x8,
        Ping                = 0x9,
        Pong                = 0xa
    }

    internal enum Mask {
        Unset   = 0x00,
        Set     = 0x01
    }
}