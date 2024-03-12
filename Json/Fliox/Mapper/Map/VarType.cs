// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Val;
using static Friflo.Json.Fliox.Mapper.Map.Var;

namespace Friflo.Json.Fliox.Mapper.Map
{
    public abstract class VarType
    {
        public   abstract string    Name            { get; }
        internal abstract Type      GetType         (in Var value);    
        internal abstract bool      IsNull          (in Var value);
        internal abstract bool      AreEqual        (in Var val1, in Var val2);
        internal abstract string    AsString        (in Var value);
        public   abstract Var       DefaultValue    { get; }
        public   abstract Var       FromObject      (object obj);
        public   abstract object    ToObject        (in Var value);
        internal virtual  object    TryGetObject    (in Var value) => null;
        internal abstract Member    CreateMember<T> (MemberInfo mi);

        public   override string    ToString() => Name;
        
        /// <summary> Method has many conditions. Cache returned VarType in case using frequently </summary>
        public static VarType FromType(Type type) {
            if (type == typeof(bool))       return TypeBool.Instance;
            if (type == typeof(char))       return TypeChar.Instance;
            
            if (type == typeof(byte))       return TypeInt8.Instance;
            if (type == typeof(short))      return TypeInt16.Instance;
            if (type == typeof(int))        return TypeInt32.Instance;
            if (type == typeof(long))       return TypeInt64.Instance;
            
            if (type == typeof(float))      return TypeFlt.Instance;
            if (type == typeof(double))     return TypeDbl.Instance;
            if (type == typeof(DateTime))   return TypeDateTime.Instance;
            
            // --- nullable
            if (type == typeof(bool?))      return TypeNullableBool.Instance;
            if (type == typeof(char?))      return TypeNullableChar.Instance;
            
            if (type == typeof(byte?))      return TypeNullableInt8.Instance;
            if (type == typeof(short?))     return TypeNullableInt16.Instance;
            if (type == typeof(int?))       return TypeNullableInt32.Instance;
            if (type == typeof(long?))      return TypeNullableInt64.Instance;
                
            if (type == typeof(float?))     return TypeNullableFlt.Instance;
            if (type == typeof(double?))    return TypeNullableDbl.Instance;
            if (type == typeof(DateTime?))  return TypeNullableDateTime.Instance;
            
            // --- NON_CLS
            if (type == typeof(sbyte))      return TypeSInt8.Instance;
            if (type == typeof(ushort))     return TypeUInt16.Instance;
            if (type == typeof(uint))       return TypeUInt32.Instance;
            if (type == typeof(ulong))      return TypeUInt64.Instance;
            
            if (type == typeof(sbyte?))     return TypeNullableSInt8.Instance;
            if (type == typeof(ushort?))    return TypeNullableUInt16.Instance;
            if (type == typeof(uint?))      return TypeNullableUInt32.Instance;
            if (type == typeof(ulong?))     return TypeNullableUInt64.Instance;

            // --- reference type
            if (type == typeof(string))     return TypeString.Instance;
            
            return TypeObject.Instance;
        }
        
        internal static string GetTypeName(Type type) {
            if (type.IsGenericType) {
                var genericArgs = type.GetGenericArguments().Select(GetTypeName);
                var idx         = type.Name.IndexOf('`');
                var typename    = (idx > 0) ? type.Name.Substring(0, idx) : type.Name;
                var args        = string.Join(", ", genericArgs);
                return $"{typename}<{args}>";
            }
            if (type.IsArray) {
                return GetTypeName(type.GetElementType()) + "[]";
            }
            return type.Name;
        }
    }
    
//  ------------------------------------- VarType implementations -------------------------------------
/// Nest concrete VarType classes in Var to make all <see cref="Var"/> fields private
public partial struct Var {

    // --- object ---
    internal sealed class TypeObject : VarType
    {
        internal static readonly    TypeObject Instance = new TypeObject();
        
        public    override  string  Name                                   => "object";
        internal  override  Type    GetType     (in Var value)             => value.obj.GetType();
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => GetString(value);
        public    override  Var     DefaultValue                           => new Var((object)null);
        public    override  Var     FromObject  (object obj)               => new Var(obj);
        public    override  object  ToObject    (in Var value)             => value.obj;
        internal  override  object  TryGetObject(in Var value)             => value.obj;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberObject<T>(mi);
        
        private static string GetString(in Var value) {
            var obj = value.obj;
            switch (obj) {
                case null:                      return "null";
                case BigInteger bigInteger:     return bigInteger.ToString();
                case DateTime   dateTime:       return DateTimeMapper.ToRFC_3339(dateTime);
                case Enum       enumObj:        return enumObj.ToString();
            }
            var type = obj.GetType();
            if (type == typeof(BigInteger?))    return ((BigInteger?)obj).Value.ToString();
            if (type == typeof(DateTime?))      return DateTimeMapper.ToRFC_3339(((DateTime?)obj).Value);

            return GetTypeName(type);
        }
    }
    
    internal sealed class TypeString : VarType
    {
        internal static readonly    TypeString Instance = new TypeString();
        
        public    override  string  Name                                   => "string";
        internal  override  Type    GetType     (in Var value)             => typeof(string);
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj != null ? $"\'{(string)value.obj}\'" : "null";
        public    override  Var     DefaultValue                           => new Var((string)null);
        public    override  Var     FromObject  (object obj)               => new Var((string)obj);
        public    override  object  ToObject    (in Var value)             => value.obj;
        internal  override  object  TryGetObject(in Var value)             => value.obj;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberString<T>(mi);
    }
    
    
    // --- long (byte, short, int, long) ---
    internal abstract class TypeLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.intern.lng.ToString();
    }
    
    internal sealed class TypeInt8 : TypeLong
    {
        private TypeInt8() { }
        internal static readonly    TypeInt8 Instance = new TypeInt8();
        
        public    override  string  Name                                   => "byte";
        internal  override  Type    GetType     (in Var value)             => typeof(byte);
        public    override  Var     DefaultValue                           => new Var((byte)default);
        public    override  Var     FromObject  (object obj)               => new Var((byte)obj);
        public    override  object  ToObject    (in Var value)             => (byte)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt8<T>(mi);
    }
    
    internal sealed class TypeInt16 : TypeLong
    {
        private TypeInt16() { }
        internal static readonly    TypeInt16 Instance = new TypeInt16();
        
        public    override  string  Name                                   => "short";
        internal  override  Type    GetType     (in Var value)             => typeof(short);
        public    override  Var     DefaultValue                           => new Var((short)default);
        public    override  Var     FromObject  (object obj)               => new Var((short)obj);
        public    override  object  ToObject    (in Var value)             => (short)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt16<T>(mi);
    }
    
    internal sealed class TypeInt32 : TypeLong
    {
        private TypeInt32() { }
        internal static readonly    TypeInt32 Instance = new TypeInt32();
        
        public    override  string  Name                                   => "int";
        internal  override  Type    GetType     (in Var value)             => typeof(int);
        public    override  Var     DefaultValue                           => new Var((int)default);
        public    override  Var     FromObject  (object obj)               => new Var((int)obj);
        public    override  object  ToObject    (in Var value)             => (int)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt32<T>(mi);
    }
    
    internal sealed class TypeInt64 : TypeLong
    {
        private TypeInt64() { }
        internal static readonly    TypeInt64 Instance = new TypeInt64();
        
        public    override  string  Name                                   => "long";
        internal  override  Type    GetType     (in Var value)             => typeof(long);
        public    override  Var     DefaultValue                           => new Var((long)default);
        public    override  Var     FromObject  (object obj)               => new Var((long)obj);
        public    override  object  ToObject    (in Var value)             => value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt64<T>(mi);
    }
    
    
    // --- nullable long (byte?, short?, int?,  long?) ---
    internal abstract class TypeNullableLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.intern.lng.ToString();

    }

    internal sealed class TypeNullableInt8 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt8 Instance = new TypeNullableInt8();
        
        public    override  string  Name                                   => "byte?";
        internal  override  Type    GetType     (in Var value)             => typeof(byte?);
        public    override  Var     DefaultValue                           => new Var((byte?)null);
        public    override  Var     FromObject  (object obj)               => new Var((byte?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (byte?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt8Null<T>(mi);
    }
    
    internal sealed class TypeNullableInt16 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt16 Instance = new TypeNullableInt16();
        
        public    override  string  Name                                   => "short?";
        internal  override  Type    GetType     (in Var value)             => typeof(short?);
        public    override  Var     DefaultValue                           => new Var((short?)null);
        public    override  Var     FromObject  (object obj)               => new Var((short?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (short?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt16Null<T>(mi);
    }
    
    internal sealed class TypeNullableInt32 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt32 Instance = new TypeNullableInt32();
        
        public    override  string  Name                                   => "int?";
        internal  override  Type    GetType     (in Var value)             => typeof(int?);
        public    override  Var     DefaultValue                           => new Var((int?)null);
        public    override  Var     FromObject  (object obj)               => new Var((int?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (int?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt32Null<T>(mi);
    }
    
    internal sealed class TypeNullableInt64 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt64 Instance = new TypeNullableInt64();
        
        public    override  string  Name                                   => "long?";
        internal  override  Type    GetType     (in Var value)             => typeof(long?);
        public    override  Var     DefaultValue                           => new Var((long?)null);
        public    override  Var     FromObject  (object obj)               => new Var((long?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (long?) value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberInt64Null<T>(mi);
    }
    
    // --- float (32 bit) ---
    internal sealed class TypeFlt : VarType
    {
        internal static readonly    TypeFlt Instance = new TypeFlt();
        
        public    override  string  Name                                   => "float";
        internal  override  Type    GetType     (in Var value)             => typeof(float);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dbl == val2.intern.dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.intern.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float)default);
        public    override  Var     FromObject  (object obj)               => new Var((float)obj);
        public    override  object  ToObject    (in Var value)             => (float)value.intern.dbl;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberFlt<T>(mi);
    }
    
    internal sealed class TypeNullableFlt : VarType
    {
        internal static readonly    TypeNullableFlt Instance = new TypeNullableFlt();
        
        public    override  string  Name                                   => "float?";
        internal  override  Type    GetType     (in Var value)             => typeof(float?);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dbl == val2.intern.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.intern.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float?)null);
        public    override  Var     FromObject  (object obj)               => new Var((float?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (float?)value.intern.dbl : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberFltNull<T>(mi);
    }
    
    // --- double (64 bit) ---
    internal sealed class TypeDbl : VarType
    {
        internal static readonly    TypeDbl Instance = new TypeDbl();
        
        public    override  string  Name                                   => "double";
        internal  override  Type    GetType     (in Var value)             => typeof(double);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dbl == val2.intern.dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.intern.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double)default);
        public    override  Var     FromObject  (object obj)               => new Var((double)obj);
        public    override  object  ToObject    (in Var value)             => value.intern.dbl;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberDbl<T>(mi);
    }
    
    internal sealed class TypeNullableDbl : VarType
    {
        internal static readonly    TypeNullableDbl Instance = new TypeNullableDbl();
        
        public    override  string  Name                                   => "double?";
        internal  override  Type    GetType     (in Var value)             => typeof(double?);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dbl == val2.intern.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.intern.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double?)null);
        public    override  Var     FromObject  (object obj)               => new Var((double?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (double?) value.intern.dbl : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberDblNull<T>(mi);
    }
    
    // --- bool ---
    internal sealed class TypeBool : VarType
    {
        internal static readonly    TypeBool Instance = new TypeBool();
        
        public    override  string  Name                                   => "bool";
        internal  override  Type    GetType     (in Var value)             => typeof(bool);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.intern.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool)default);
        public    override  Var     FromObject  (object obj)               => new Var((bool)obj);
        public    override  object  ToObject    (in Var value)             => value.intern.lng != 0;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberBool<T>(mi);
    }
    
    internal sealed class TypeNullableBool : VarType
    {
        internal static readonly    TypeNullableBool Instance = new TypeNullableBool();
        
        public    override  string  Name                                   => "bool?";
        internal  override  Type    GetType     (in Var value)             => typeof(bool?);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.intern.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool?)null);
        public    override  Var     FromObject  (object obj)               => new Var((bool?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (bool?)(value.intern.lng != 0) : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberBoolNull<T>(mi);
    }
    
    
    // --- char ---
    internal sealed class TypeChar : VarType
    {
        internal static readonly    TypeChar Instance = new TypeChar();
        
        public    override  string  Name                                   => "char";
        internal  override  Type    GetType     (in Var value)             => typeof(char);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => $"'{(char)value.intern.lng}'";
        public    override  Var     DefaultValue                           => new Var((char)default);
        public    override  Var     FromObject  (object obj)               => new Var((char)obj);
        public    override  object  ToObject    (in Var value)             => (char)value.intern.lng;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberChar<T>(mi);
    }
    
    internal sealed class TypeNullableChar : VarType
    {
        internal static readonly    TypeNullableChar Instance = new TypeNullableChar();
        
        public    override  string  Name                                   => "char?";
        internal  override  Type    GetType     (in Var value)             => typeof(char?);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.lng == val2.intern.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : $"'{(char)value.intern.lng}'";
        public    override  Var     DefaultValue                           => new Var((char?)null);
        public    override  Var     FromObject  (object obj)               => new Var((char?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (char?)value.intern.lng : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberCharNull<T>(mi);
    }
    
    // --- DateTime ---
    internal sealed class TypeDateTime : VarType
    {
        internal static readonly    TypeDateTime Instance = new TypeDateTime();
        
        public    override  string  Name                                   => "DateTime";
        internal  override  Type    GetType     (in Var value)             => typeof(DateTime);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dt == val2.intern.dt;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.intern.dt.ToString(Bytes.DateTimeFormat);
        public    override  Var     DefaultValue                           => new Var((DateTime)default);
        public    override  Var     FromObject  (object obj)               => new Var((DateTime)obj);
        public    override  object  ToObject    (in Var value)             => value.DateTime;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberDateTime<T>(mi);
    }
    
    internal sealed class TypeNullableDateTime : VarType
    {
        internal static readonly    TypeNullableDateTime Instance = new TypeNullableDateTime();
        
        public    override  string  Name                                   => "DateTime?";
        internal  override  Type    GetType     (in Var value)             => typeof(DateTime?);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.intern.dt == val2.intern.dt && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.intern.dt.ToString(Bytes.DateTimeFormat);
        public    override  Var     DefaultValue                           => new Var((DateTime?)null);
        public    override  Var     FromObject  (object obj)               => new Var((DateTime?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? value.DateTimeNull : null;
        internal  override  Member  CreateMember<T>(MemberInfo mi)         => new MemberDateTimeNull<T>(mi);
    }
}
}