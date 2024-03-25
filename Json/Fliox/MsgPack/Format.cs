// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo
namespace Friflo.Json.Fliox.MsgPack
{

    /// <summary>
    /// Spec: [msgpack/spec.md · msgpack/msgpack] https://github.com/msgpack/msgpack/blob/master/spec.md <br/>
    /// Online converters:<br/>
    /// hex     [msgpack-lite demo] https://kawanet.github.io/msgpack-lite/
    /// dec     [MsgPack Converter | MsgPack to JSON Decoder and Encoder] https://ref45638.github.io/msgpack-converter/
    /// </summary>
    public enum MsgFormat
    {
        fixintPos   = 0x00, fixintPosMax    = 0x7f,     // 0xxxxxxx          range: [0, 127]
        fixmap      = 0x80, fixmapMax       = 0x8f,     // 1000xxxx (8x)     + N*2 objects
        fixarray    = 0x90, fixarrayMax     = 0x9f,     // 1001xxxx (9x)     + N   objects
        fixstr      = 0xa0, fixstrMax       = 0xbf,     // 101xxxxx (Ax, Bx) + N   bytes
        
        // --- null
        nil         = 0xc0,
        unused      = 0xc1,
        
        // --- boolean
        False       = 0xc2,
        True        = 0xc3,
        
        // --- bin
        bin8        = 0xc4,     // + 1 byte  length(N) + N bytes
        bin16       = 0xc5,     // + 2 bytes length(N) + N bytes
        bin32       = 0xc6,     // + 4 bytes length(N) + N bytes
        
        // --- ext
        ext8        = 0xc7,     // + 1 byte  length(N) + 1 byte type + N bytes
        ext16       = 0xc8,     // + 2 bytes length(N) + 1 byte type + N bytes
        ext32       = 0xc9,     // + 4 bytes length(N) + 1 byte type + N bytes
        
        // --- float
        float32     = 0xca,     // + 4 bytes data
        float64     = 0xcb,     // + 8 bytes data
        
        // --- int
        uint8       = 0xcc,     // + 1 byte  data
        uint16      = 0xcd,     // + 2 bytes data
        uint32      = 0xce,     // + 4 bytes data
        uint64      = 0xcf,     // + 8 bytes data
        
        int8        = 0xd0,     // + 1 byte  data
        int16       = 0xd1,     // + 2 bytes data
        int32       = 0xd2,     // + 4 bytes data
        int64       = 0xd3,     // + 8 bytes data
        
        // --- fixext
        fixext1     = 0xd4,     // + 1 byte type +  1 byte  data
        fixext2     = 0xd5,     // + 1 byte type +  2 bytes data
        fixext4     = 0xd6,     // + 1 byte type +  4 bytes data
        fixext8     = 0xd7,     // + 1 byte type +  8 bytes data
        fixext16    = 0xd8,     // + 1 byte type + 16 bytes data
        
        // --- string
        str8        = 0xd9,     // + 1 byte  length(N) + N bytes
        str16       = 0xda,     // + 2 bytes length(N) + N bytes
        str32       = 0xdb,     // + 4 bytes length(N) + N bytes
        
        // --- array
        array16     = 0xdc,     // + 2 bytes length(N) + N objects
        array32     = 0xdd,     // + 4 bytes length(N) + N objects
        
        // --- map
        map16       = 0xde,     // + 2 bytes length(N) + N*2 objects
        map32       = 0xdf,     // + 4 bytes length(N) + N*2 objects
        
        // --- fixint - negative 
        fixintNeg   = 0xe0, fixintNegMax = 0xff,     // 111xxxxx (Ex, Fx)  range: [-32(0xE0), -1(0xFF)]
        //
        //
        root        = 0x100     // Note: not part of spec
    }
}