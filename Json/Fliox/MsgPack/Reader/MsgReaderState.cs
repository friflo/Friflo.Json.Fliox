// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    [Flags]
    public enum MsgReaderState
    {
        Ok              = 0,
        /// <summary>
        /// bit set in case of range errors for <see cref="ExpectUint8"/>, ..., <see cref="ExpectFloat64"/>
        /// </summary>
        RangeError      = 0b_00001_0000, 
        //
        /// used to mask subsequent states / errors
        Mask            = 0x0f, 
        // --- expected state / error
        ExpectArray     = 0x01,
        ExpectByteArray = 0x02,
        ExpectBool      = 0x03,
        ExpectString    = 0x04,
        ExpectObject    = 0x05,
        ExpectKeyString = 0x06,
        //
        ExpectUint8     = 0x07,
        ExpectInt16     = 0x08,
        ExpectInt32     = 0x09,
        ExpectInt64     = 0x0a,
        ExpectFloat32   = 0x0b,
        ExpectFloat64   = 0x0c,
        // --- general errors
        UnexpectedEof   = 0x0d,
        UnsupportedType = 0x0e,
    }
}