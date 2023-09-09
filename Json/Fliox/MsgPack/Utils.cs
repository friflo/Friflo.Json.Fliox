// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.RegularExpressions;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

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
                case <= fixintPosMax:                               return "+fixint";
                case >= fixstr    and <= fixstrMax:                 return "fixstr";
                case >= fixmap    and <= fixmapMax:                 return "fixmap";
                case >= fixarray  and <= fixarrayMax:               return "fixarray";
                case >= fixintNeg and <= MsgFormat.fixintNegMax:    return "-fixint";
                default:
                    return type.ToString();
            }
        }
        
        internal static string GetDataDec(byte[] data, int len) {
            var sb = new StringBuilder();
            for (int n = 0; n < len; n++) {
                sb.Append(data[n]);
                sb.Append(", ");
            }
            if (len > 0) sb.Length -= 2;
            return sb.ToString();
        }
        
        internal static string GetDataHex(byte[] data, int len) {
            var sb = new StringBuilder();
            for (int n = 0; n < len; n++) {
                sb.Append($"{data[n]:X2}");
                sb.Append(' ');
            }
            if (len > 0) sb.Length--;
            return sb.ToString();
        }
        
        /// <summary> Convert hex to JSON with [Online msgpack converter] <br/> https://msgpack.solder.party/  </summary>
        public static ReadOnlySpan<byte> HexToSpan(string hex) {
            if (hex.Length == 0) {
                return new ReadOnlySpan<byte>(Array.Empty<byte>());
            }
            var items = hex.Split(' ');
            var result = new byte[items.Length];
            for (int n = 0; n < items.Length; n++) {
                result[n] = Convert.ToByte(items[n], 16);
            }
            return new ReadOnlySpan<byte>(result);
        }
        
        public static ReadOnlySpan<byte> ByteToSpan(MsgFormat value) {
            return new ReadOnlySpan<byte>(new byte[] { (byte)value });
        }
        
        /// <summary> Convert hex to JSON with [Online msgpack converter]<br/>https://msgpack.solder.party/  </summary>
        public static string HexNorm(string hex) {
            return WhiteSpace.Replace(hex, " " );
        }
        
        private static readonly Regex WhiteSpace = new Regex(@"\s+");
    }
}