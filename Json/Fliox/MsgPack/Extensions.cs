// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Fliox.MsgPack
{
    public static class MsgExtensions
    {
        public static string DataHex(this ReadOnlySpan<byte> data) {
            return MsgPackUtils.GetDataHex(data);
        }
        
        public static string DataString(this ReadOnlySpan<byte> data) {
            return MsgPackUtils.SpanToString(data);
        }
        
        public static ReadOnlySpan<byte> String2Span(this string value) {
            var bytes = Encoding.UTF8.GetBytes(value);
            return new ReadOnlySpan<byte>(bytes);
        }
    }
}