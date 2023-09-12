// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.MsgPack
{
    public static class Extensions
    {
        public static string DataHex(this ReadOnlySpan<byte> data) {
            return MsgPackUtils.GetDataHex(data);
        }
        
        public static string DataString(this ReadOnlySpan<byte> data) {
            return MsgPackUtils.SpanToString(data);
        }
    }
}