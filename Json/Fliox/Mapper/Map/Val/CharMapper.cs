// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Schema.Definition;
using static Friflo.Json.Fliox.Mapper.Diff.DiffType;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // A char is written as a JSON string of exactly one UTF-16 code unit: "a", "\"", "\n", "\u0000".
    internal sealed class CharMatcher : ITypeMatcher {
        public static readonly CharMatcher Instance = new CharMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(char))
                return new CharMapper (config, type);
            if (type == typeof(char?))
                return new NullableCharMapper (config, type);
            return null;
        }
    }
    
    internal static class CharJson
    {
        internal static void Write(ref Writer writer, char value) {
            if (char.IsSurrogate(value)) {
                // half of a surrogate pair has no UTF-8 encoding: write the escape, which is valid JSON
                writer.bytes.AppendChar('"');
                writer.bytes.AppendString("\\u" + ((int)value).ToString("x4"));
                writer.bytes.AppendChar('"');
                return;
            }
            Span<char> chars = stackalloc char[1];
            chars[0] = value;
            Utf8JsonWriter.AppendEscString(ref writer.bytes, chars);
        }
        
        internal static bool TryRead(ref Reader reader, out char result) {
            var chars = reader.parser.value.GetChars(ref reader.charBuf, out int len);
            if (len == 1) {
                result = chars[0];
                return true;
            }
            // The parser decodes an escaped half of a surrogate pair ("\ud800") as the three bytes UTF-8 would use if
            // surrogates were encodable. They are not valid UTF-8, so the chars above are replacement characters.
            var span = reader.parser.value.AsSpan();
            if (span.Length == 3 && span[0] == 0xED && span[1] >= 0xA0 && span[1] <= 0xBF && (span[2] & 0xC0) == 0x80) {
                result = (char)(((span[0] & 0x0F) << 12) | ((span[1] & 0x3F) << 6) | (span[2] & 0x3F));
                return true;
            }
            result = default;
            return false;
        }
    }
    
    internal sealed class CharMapper : TypeMapper<char>
    {
        public override StandardTypeId  StandardTypeId      => StandardTypeId.String;
        public override string          StandardTypeName    => "string";
        public override string  DataTypeName()              => "char";
        public override bool    IsNull(ref char value)      => false;

        public CharMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void        Write   (ref Writer writer, char slot)              => CharJson.Write(ref writer, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Char);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Char, right.Char);
        public override DiffType    Diff    (Differ differ, char left, char right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (char value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Char, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.Char);
        
        public override char Read(ref Reader reader, char slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            if (!CharJson.TryRead(ref reader, out var result))
                return reader.ErrorMsg<char>("Expected a string of exactly one character for char. value: ", reader.parser.value.AsString(), out success);
            success = true;
            return result;
        }
    }
    
    internal sealed class NullableCharMapper : TypeMapper<char?>
    {
        public override StandardTypeId  StandardTypeId      => StandardTypeId.String;
        public override string          StandardTypeName    => "string";
        public override string  DataTypeName()              => "char?";
        public override bool    IsNull(ref char? value)     => !value.HasValue;

        public NullableCharMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void        Write   (ref Writer writer, char? slot)             => CharJson.Write(ref writer, (char)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.CharNull);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.CharNull, right.CharNull);
        public override DiffType    Diff    (Differ differ, char? left, char? right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (char? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.CharNull, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.CharNull);
        
        public override char? Read(ref Reader reader, char? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            if (!CharJson.TryRead(ref reader, out var result))
                return reader.ErrorMsg<char?>("Expected a string of exactly one character for char?. value: ", reader.parser.value.AsString(), out success);
            success = true;
            return result;
        }
    }
}
