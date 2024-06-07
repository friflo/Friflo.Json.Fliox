// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    
    internal sealed class DateTimeMatcher  : ITypeMatcher {
        public static readonly DateTimeMatcher Instance = new DateTimeMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(DateTime))
                return new DateTimeMapper (config, type);
            if (type == typeof(DateTime?))
                return new NullableDateTimeMapper (config, type);
            return null;
        }
    }
    
    internal sealed class DateTimeMapper : TypeMapper<DateTime>
    {
        public override StandardTypeId  StandardTypeId              => StandardTypeId.DateTime;
        public override string          StandardTypeName            => "DateTime";
        public override string          DataTypeName()              => "DateTime";
        public override bool            IsNull(ref DateTime value)  => false;
        
        public DateTimeMapper(StoreConfig config, Type type) :
            base (config, type, false, true) {
        }
        
        public override void        WriteVar(ref Writer writer, in Var value)               => Write(ref writer, value.DateTime);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)      => Diff(differ, left.DateTime, right.DateTime);
        public override DiffType    Diff    (Differ differ, DateTime left, DateTime right)  => left == right ? DiffType.Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (DateTime value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.DateTime, out success));
        public override void        CopyVar (in Var src, ref Var dst)                       => dst = new Var(src.DateTime);

        public override void Write(ref Writer writer, DateTime value) {
            writer.WriteDateTime(value);
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime Read(ref Reader reader, DateTime slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            var str = value.AsString();
            if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result)) {
                return reader.ErrorMsg<DateTime>("Failed parsing DateTime. value: ", str, out success);
            }
            success = true;
            return result;
        }
        
        /// <summary>uses same format as <see cref="Bytes.AppendDateTime"/></summary>
        public static string ToRFC_3339(in DateTime value) {
            var utc = value.ToUniversalTime();
            return utc.ToString(Bytes.DateTimeFormat);
        }
    }
    
    internal sealed class NullableDateTimeMapper : TypeMapper<DateTime?>
    {
        public override StandardTypeId  StandardTypeId              => StandardTypeId.DateTime;
        public override string          StandardTypeName            => "DateTime";
        public override string          DataTypeName()              => "DateTime?";
        public override bool            IsNull(ref DateTime? value) => !value.HasValue;
        
        public override void        WriteVar(ref Writer writer, in Var value)               => Write(ref writer, value.DateTimeNull);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)      => Diff(differ, left.DateTimeNull, right.DateTimeNull);
        public override DiffType    Diff    (Differ differ, DateTime? left, DateTime? right)=> left == right ? DiffType.Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (DateTime? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.DateTimeNull, out success));
        public override void        CopyVar (in Var src, ref Var dst)                       => dst = new Var(src.DateTimeNull);
        
        public NullableDateTimeMapper(StoreConfig config, Type type) :
            base (config, type, true, true) {
        }

        public override void Write(ref Writer writer, DateTime? value) {
            if (value.HasValue) {
                writer.WriteDateTime(value.Value);
            } else {
                writer.AppendNull();
            }
        }

        // ReSharper disable once RedundantAssignment
        public override DateTime? Read(ref Reader reader, DateTime? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            ref var value = ref reader.parser.value;
            var str = value.AsString();
            if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result)) {
                return reader.ErrorMsg<DateTime?>("Failed parsing DateTime. value: ", str, out success);
            }
            success = true;
            return result;
        }
    }
}
