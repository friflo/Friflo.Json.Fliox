// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static System.Buffers.Binary.BinaryPrimitives;

// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
namespace Friflo.Json.Fliox.MsgPack
{
    public static class MsgPackUtils
    {
        internal static string Name(MsgFormat type)
        {
            switch (type) {
                case nil:       return "nil";
                case unused:    return "unused";
        
                // --- boolean
                case False:     return "false";
                case True:      return "true";
        
                // --- bin
                case bin8:      return "bin8";
                case bin16:     return "bin16";
                case bin32:     return "bin32";
        
                // --- ext
                case ext8:      return "ext8";
                case ext16:     return "ext16";
                case ext32:     return "ext32";
        
                // --- float
                case float32:   return "float32";
                case float64:   return "float64";
        
                // --- int
                case uint8:     return "uint8";
                case uint16:    return "uint16";
                case uint32:    return "uint32";
                case uint64:    return "uint64";
        
                case int8:      return "int8";
                case int16:     return "int16";
                case int32:     return "int32";
                case int64:     return "int64";
        
                // --- fixext
                case fixext1:   return "fixext1";
                case fixext2:   return "fixext2";
                case fixext4:   return "fixext4";
                case fixext8:   return "fixext8";
                case fixext16:  return "fixext16";
        
                // --- string
                case str8:      return "str8";
                case str16:     return "str16";
                case str32:     return "str32";
        
                // --- array
                case array16:   return "array16";
                case array32:   return "array32";
        
                // --- map
                case map16:     return "map16";
                case map32:     return "map32";
                
                // --- fix*
                case <= fixintPosMax:                   return "+fixint";
                case >= fixstr    and <= fixstrMax:     return "fixstr";
                case >= fixmap    and <= fixmapMax:     return "fixmap";
                case >= fixarray  and <= fixarrayMax:   return "fixarray";
                case >= fixintNeg and <= fixintNegMax:  return "-fixint";
                //
                case root:      return "root";
                default:
                    return type.ToString();
            }
        }
        
        internal static string Error(MsgReaderState state)
        {
            var error = state & Mask;
            switch (error)
            {
                case Ok:                return "OK";
                //
                case UnexpectedEof:     return "unexpected EOF";
                //
                case ExpectArray:       return "expect array or null";
                case ExpectByteArray:   return "expect byte[] or null";
                case ExpectBool:        return "expect bool";
                case ExpectString:      return "expect string or null";
                case ExpectObject:      return "expect object or null";
                case ExpectKeyString:   return "expect key type string";
                //
                case ExpectUint8:       return "expect uint8";
                case ExpectInt16:       return "expect int16";
                case ExpectInt32:       return "expect int32";
                case ExpectInt64:       return "expect int64";
                case ExpectFloat32:     return "expect float32";
                case ExpectFloat64:     return "expect float64";
                //
                case UnsupportedType:   return "unsupported type";
                
                default:                return state.ToString();
            }
        }
        
        private static readonly NumberFormatInfo NumberFormat = CultureInfo.InvariantCulture.NumberFormat;

        internal static void AppendValue(StringBuilder sb, ReadOnlySpan<byte> data, MsgFormat type, int cur)
        {
            switch (type)
            {
                case <= fixintPosMax:   sb.Append((byte)type);                                  break;
                case >= fixintNeg:      sb.Append((sbyte)((int)type - 256));                    break;
                //
                case int8:              sb.Append((sbyte)data[cur + 1]);                        break;
                case int16:             sb.Append(ReadInt16BigEndian(data.Slice(cur + 1, 2)));  break;
                case int32:             sb.Append(ReadInt32BigEndian(data.Slice(cur + 1, 4)));  break;
                case int64:             sb.Append(ReadInt64BigEndian(data.Slice(cur + 1, 8)));  break;
                //
                case uint8:             sb.Append(data[cur + 1]);                               break;
                case uint16:            sb.Append(ReadUInt16BigEndian(data.Slice(cur + 1, 2))); break;
                case uint32:            sb.Append(ReadUInt32BigEndian(data.Slice(cur + 1, 4))); break;
                case uint64:            sb.Append(ReadUInt64BigEndian(data.Slice(cur + 1, 8))); break;
                //
                case float32: {
#if !NETSTANDARD2_0
                    var flt = BitConverter.Int32BitsToSingle(ReadInt32BigEndian(data.Slice(cur + 1, 4)));
                    sb.Append(flt.ToString(NumberFormat));
#endif
                    break;
                }
                case float64: {
                    var dbl = BitConverter.Int64BitsToDouble(ReadInt64BigEndian(data.Slice(cur + 1, 8)));
                    sb.Append(dbl.ToString(NumberFormat));
                    break;
                }
            }
        }
        
        internal static string SpanToString(in ReadOnlySpan<byte> span) {
#if NETSTANDARD2_0
            throw new NotSupportedException(); 
#else
            return Encoding.UTF8.GetString(span);
#endif
        }
        
        internal static string GetDataDec(ReadOnlySpan<byte> data) {
            var sb  = new StringBuilder();
            var len = data.Length;
            for (int n = 0; n < len; n++) {
                sb.Append(data[n]);
                sb.Append(", ");
            }
            if (len > 0) sb.Length -= 2;
            return sb.ToString();
        }
        
        internal static string GetDataHex(ReadOnlySpan<byte> data) {
            var sb  = new StringBuilder();
            var len = data.Length;
            for (int n = 0; n < len; n++) {
                sb.Append($"{data[n]:X2}");
                sb.Append(' ');
            }
            if (len > 0) sb.Length--;
            return sb.ToString();
        }
        
        /// <summary> Convert hex to JSON with [msgpack-lite demo] https://kawanet.github.io/msgpack-lite/ </summary>
        public static ReadOnlySpan<byte> HexToSpan(string hex) {
            if (hex.Length == 0) {
                return new ReadOnlySpan<byte>(Array.Empty<byte>());
            }
            var items = WhiteSpace.Split(hex);
            var result = new byte[items.Length];
            for (int n = 0; n < items.Length; n++) {
                result[n] = Convert.ToByte(items[n], 16);
            }
            return new ReadOnlySpan<byte>(result);
        }
        
        public static ReadOnlySpan<byte> ByteToSpan(MsgFormat value) {
            return new ReadOnlySpan<byte>(new byte[] { (byte)value });
        }
        
        /// <summary> Convert hex to JSON with [msgpack-lite demo] https://kawanet.github.io/msgpack-lite/ </summary>
        public static string HexNorm(string hex) {
            return WhiteSpace.Replace(hex, " " );
        }
        
        private static readonly Regex WhiteSpace = new Regex(@"\s+");
    }
}