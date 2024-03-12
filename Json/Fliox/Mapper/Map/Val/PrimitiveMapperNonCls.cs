// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using static Friflo.Json.Fliox.Mapper.Diff.DiffType;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable RedundantAssignment
// ReSharper disable PossibleInvalidOperationException
namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // NON_CLS - whole file
    // ---------------------------------------------------------------------------- ulong
    internal sealed class ULongMatcher : ITypeMatcher {
        public static readonly ULongMatcher Instance = new ULongMatcher();
                
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(ulong))
                return new ULongMapper (config, type);
            if (type == typeof(ulong?))
                return new NullableULongMapper (config, type);
            return null;
        }
    }
    internal sealed class ULongMapper : TypeMapper<ulong> {
        public override string  DataTypeName()          => "ulong";
        public override bool    IsNull(ref ulong value)  => false;

        public ULongMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, ulong slot)             => writer.format.AppendULong(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt64);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt64, right.UInt64);
        public override DiffType    Diff    (Differ differ, ulong left, ulong right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (ulong value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt64, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt64);
        
        public override ulong Read(ref Reader reader, ulong slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsULong(out success);
        }
    }
    internal sealed class NullableULongMapper : TypeMapper<ulong?> {
        public override string  DataTypeName()          => "ulong?";
        public override bool    IsNull(ref ulong? value) => !value.HasValue;

        public NullableULongMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, ulong? slot)            => writer.format.AppendULong(ref writer.bytes, (ulong)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt64Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt64Null, right.UInt64Null);
        public override DiffType    Diff    (Differ differ, ulong? left, ulong? right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (ulong? value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt64Null, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt64Null);
        
        public override ulong? Read(ref Reader reader, ulong? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsULong(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- uint
    internal sealed class UIntMatcher : ITypeMatcher {
        public static readonly UIntMatcher Instance = new UIntMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(uint))
                return new UIntMapper (config, type); 
            if (type == typeof(uint?))
                return new NullableUIntMapper(config, type);
            return null;
        }
    }
    internal sealed class UIntMapper : TypeMapper<uint> {
        public override string  DataTypeName()          => "uint";
        public override bool    IsNull(ref uint value)   => false;

        public UIntMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, uint slot)              => writer.format.AppendULong(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt32);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt32, right.UInt32);
        public override DiffType    Diff    (Differ differ, uint left, uint right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (uint value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt32, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt32);
        
        public override uint Read(ref Reader reader, uint slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsUInt(out success);
        }
    }
    internal sealed class NullableUIntMapper : TypeMapper<uint?> {
        public override string  DataTypeName()          => "uint?";
        public override bool    IsNull(ref uint? value)  => !value.HasValue;

        public NullableUIntMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, uint? slot)             => writer.format.AppendULong(ref writer.bytes, (uint)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt32Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt32Null, right.UInt32Null);
        public override DiffType    Diff    (Differ differ, uint? left, uint? right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (uint? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt32Null, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt32Null);
        
        public override uint? Read(ref Reader reader, uint? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsUInt(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- ushort
    internal sealed class UShortMatcher : ITypeMatcher {
        public static readonly UShortMatcher Instance = new UShortMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(ushort))
                return new UShortMapper (config, type);
            if (type == typeof(ushort?))
                return new NullableUShortMapper (config, type);
            return null;
        }
    }
    internal sealed class UShortMapper : TypeMapper<ushort> {
        public override string  DataTypeName()          => "ushort";
        public override bool    IsNull(ref ushort value) => false;
        
        public UShortMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void        Write   (ref Writer writer, ushort slot)            => writer.format.AppendInt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt16);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt16, right.UInt16);
        public override DiffType    Diff    (Differ differ, ushort left, ushort right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (ushort value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt16, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt16);
        
        public override ushort Read(ref Reader reader, ushort slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsUShort(out success);
        }
    }
    internal sealed class NullableUShortMapper : TypeMapper<ushort?> {
        public override string  DataTypeName()              => "ushort?";
        public override bool    IsNull(ref ushort? value)    => !value.HasValue;
        
        public NullableUShortMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void        Write   (ref Writer writer, ushort? slot)           => writer.format.AppendInt(ref writer.bytes, (ushort)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.UInt16Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.UInt16Null, right.UInt16Null);
        public override DiffType    Diff    (Differ differ, ushort? left, ushort? right)=> left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (ushort? value)                             => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.UInt16Null, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.UInt16Null);
        
        public override ushort? Read(ref Reader reader, ushort? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsUShort(out success);
        }
    }
    
    
    // ---------------------------------------------------------------------------- sbyte
    internal sealed class SByteMatcher : ITypeMatcher {
        public static readonly SByteMatcher Instance = new SByteMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(sbyte))
                return new SByteMapper (config, type); 
            if (type == typeof(sbyte?))
                return new NullableSByteMapper(config, type);
            return null;
        }
    }
    internal sealed class SByteMapper : TypeMapper<sbyte> {
        public override string  DataTypeName()          => "sbyte";
        public override bool    IsNull(ref sbyte value)  => false;

        public SByteMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, sbyte slot)             => writer.format.AppendInt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.SInt8);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.SInt8, right.SInt8);
        public override DiffType    Diff    (Differ differ, sbyte left, sbyte right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (sbyte value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.SInt8, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.SInt8);
        
        public override sbyte Read(ref Reader reader, sbyte slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsSByte(out success);
        }
    }
    internal sealed class NullableSByteMapper : TypeMapper<sbyte?> {
        public override string  DataTypeName()          => "sbyte?";
        public override bool    IsNull(ref sbyte? value) => !value.HasValue;

        public NullableSByteMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, sbyte? slot)            => writer.format.AppendInt(ref writer.bytes, (sbyte)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.SInt8Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.SInt8Null, right.SInt8Null);
        public override DiffType    Diff    (Differ differ, sbyte? left, sbyte? right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (sbyte? value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.SInt8Null, out success));
        public override void        CopyVar (in Var src, ref Var dst)                   => dst = new Var(src.SInt8Null);
        
        public override sbyte? Read(ref Reader reader, sbyte? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsSByte(out success);
        }
    }
}