// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Map
{

// NON_CLS - whole file
//  ------------------------------------- VarType implementations -------------------------------------
/// Nest concrete VarType classes in Var to make all <see cref="Var"/> fields private
public partial struct Var {

    
    
    // --- long (sbyte, ushort, uint, ulong) ---
    internal sealed class TypeSInt8 : TypeLong
    {
        private TypeSInt8() { }
        internal static readonly    TypeSInt8 Instance = new TypeSInt8();
        
        public    override  string  Name                                   => "sbyte";
        internal  override  Type    GetType     (in Var value)             => typeof(sbyte);
        public    override  Var     DefaultValue                           => new Var((sbyte)default);
        public    override  Var     FromObject  (object obj)               => new Var((sbyte)obj);
        public    override  object  ToObject    (in Var value)             => (sbyte)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberSInt8<T>(mi);
    }
    
    internal sealed class TypeUInt16 : TypeLong
    {
        private TypeUInt16() { }
        internal static readonly    TypeUInt16 Instance = new TypeUInt16();
        
        public    override  string  Name                                   => "ushort";
        internal  override  Type    GetType     (in Var value)             => typeof(ushort);
        public    override  Var     DefaultValue                           => new Var((ushort)default);
        public    override  Var     FromObject  (object obj)               => new Var((ushort)obj);
        public    override  object  ToObject    (in Var value)             => (ushort)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt16<T>(mi);
    }
    
    internal sealed class TypeUInt32 : TypeLong
    {
        private TypeUInt32() { }
        internal static readonly    TypeUInt32 Instance = new TypeUInt32();
        
        public    override  string  Name                                   => "uint";
        internal  override  Type    GetType     (in Var value)             => typeof(uint);
        public    override  Var     DefaultValue                           => new Var((uint)default);
        public    override  Var     FromObject  (object obj)               => new Var((uint)obj);
        public    override  object  ToObject    (in Var value)             => (uint)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt32<T>(mi);
    }
    
    internal sealed class TypeUInt64 : TypeLong
    {
        private TypeUInt64() { }
        internal static readonly    TypeUInt64 Instance = new TypeUInt64();
        
        public    override  string  Name                                   => "ulong";
        internal  override  Type    GetType     (in Var value)             => typeof(ulong);
        public    override  Var     DefaultValue                           => new Var((ulong)default);
        public    override  Var     FromObject  (object obj)               => new Var((ulong)obj);
        public    override  object  ToObject    (in Var value)             => (ulong)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt64<T>(mi);
        internal  override  string  AsString    (in Var value)             => ((ulong)value.intern.lng).ToString();
    }
    
    
    // --- nullable long (sbyte?, ushort?, uint?,  ulong?) ---
    internal sealed class TypeNullableSInt8 : TypeNullableLong
    {
        internal static readonly    TypeNullableSInt8 Instance = new TypeNullableSInt8();
        
        public    override  string  Name                                   => "sbyte?";
        internal  override  Type    GetType     (in Var value)             => typeof(sbyte?);
        public    override  Var     DefaultValue                           => new Var((sbyte?)null);
        public    override  Var     FromObject  (object obj)               => new Var((sbyte?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (sbyte?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberSInt8Null<T>(mi);
    }
    
    internal sealed class TypeNullableUInt16 : TypeNullableLong
    {
        internal static readonly    TypeNullableUInt16 Instance = new TypeNullableUInt16();
        
        public    override  string  Name                                   => "ushort?";
        internal  override  Type    GetType     (in Var value)             => typeof(ushort?);
        public    override  Var     DefaultValue                           => new Var((ushort?)null);
        public    override  Var     FromObject  (object obj)               => new Var((ushort?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (ushort?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt16Null<T>(mi);
    }
    
    internal sealed class TypeNullableUInt32 : TypeNullableLong
    {
        internal static readonly    TypeNullableUInt32 Instance = new TypeNullableUInt32();
        
        public    override  string  Name                                   => "uint?";
        internal  override  Type    GetType     (in Var value)             => typeof(uint?);
        public    override  Var     DefaultValue                           => new Var((uint?)null);
        public    override  Var     FromObject  (object obj)               => new Var((uint?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (uint?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt32Null<T>(mi);
    }
    
    internal sealed class TypeNullableUInt64 : TypeNullableLong
    {
        internal static readonly    TypeNullableUInt64 Instance = new TypeNullableUInt64();
        
        public    override  string  Name                                   => "ulong?";
        internal  override  Type    GetType     (in Var value)             => typeof(ulong?);
        public    override  Var     DefaultValue                           => new Var((ulong?)null);
        public    override  Var     FromObject  (object obj)               => new Var((ulong?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (ulong?) value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberUInt64Null<T>(mi);
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : ((ulong)value.intern.lng).ToString();
    }

}
}