// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using static Friflo.Json.Fliox.Mapper.Map.Var;

namespace Friflo.Json.Fliox.Mapper.Map
{
    public abstract class VarType
    {
        public   abstract string    Name        { get; }
        internal abstract bool      IsNull      (in Var value);
        internal abstract bool      AreEqual    (in Var val1, in Var val2);
        internal abstract string    AsString    (in Var value);
        public   abstract Var       DefaultValue{ get; }
        public   abstract Var       FromObject  (object obj);
        public   abstract object    ToObject    (in Var value);
        internal virtual  object    TryGetObject(in Var value) => null;

        public   override string    ToString() => Name;
        
        /// <summary> Method has many conditions. Cache returned VarType in case using frequently </summary>
        public static VarType FromType(Type type) {
            if (type == typeof(bool))   return TypeBool.Instance;
            if (type == typeof(char))   return TypeChar.Instance;
            
            if (type == typeof(byte))   return TypeInt8.Instance;
            if (type == typeof(short))  return TypeInt16.Instance;
            if (type == typeof(int))    return TypeInt32.Instance;
            if (type == typeof(long))   return TypeInt64.Instance;
            
            if (type == typeof(float))  return TypeFlt.Instance;
            if (type == typeof(double)) return TypeDbl.Instance;
            
            // --- nullable
            if (type == typeof(bool?))  return TypeNullableBool.Instance;
            if (type == typeof(char?))  return TypeNullableChar.Instance;
            
            if (type == typeof(byte?))  return TypeNullableInt8.Instance;
            if (type == typeof(short?)) return TypeNullableInt16.Instance;
            if (type == typeof(int?))   return TypeNullableInt32.Instance;
            if (type == typeof(long?))  return TypeNullableInt64.Instance;
            
            if (type == typeof(float?)) return TypeNullableFlt.Instance;
            if (type == typeof(double?))return TypeNullableDbl.Instance;

            // --- reference type
            if (type == typeof(string)) return TypeString.Instance;

            return TypeObject.Instance;
        }
    }
    
//  ------------------------------------- VarType implementations -------------------------------------
/// Nest concrete VarType classes in Var to make all <see cref="Var"/> fields private
public partial struct Var {
        
    // --- object ---
    internal sealed class TypeObject : VarType
    {
        internal static readonly    TypeObject Instance = new TypeObject();
        
        public    override  string  Name     => "object";
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj != null ? value.obj.ToString() : "null";
        public    override  Var     DefaultValue                           => new Var((object)null);
        public    override  Var     FromObject  (object obj)               => new Var(obj);
        public    override  object  ToObject    (in Var value)             => value.obj;
        internal  override  object  TryGetObject(in Var value)             => value.obj;
    }
    
    internal sealed class TypeString : VarType
    {
        internal static readonly    TypeString Instance = new TypeString();
        
        public    override  string  Name     => "string";
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj != null ? $"\"{(string)value.obj}\"" : "null";
        public    override  Var     DefaultValue                           => new Var((string)null);
        public    override  Var     FromObject  (object obj)               => new Var((string)obj);
        public    override  object  ToObject    (in Var value)             => value.obj;
        internal  override  object  TryGetObject(in Var value)             => value.obj;
    }
    
    
    // --- long (byte, short, int, long) ---
    internal abstract class TypeLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.lng.ToString();
    }
    
    internal sealed class TypeInt8 : TypeLong
    {
        private TypeInt8() { }
        internal static readonly    TypeInt8 Instance = new TypeInt8();
        
        public    override  string  Name        => "byte";
        public    override  Var     DefaultValue                           => new Var((byte)default);
        public    override  Var     FromObject  (object obj)               => new Var((byte)obj);
        public    override  object  ToObject    (in Var value)             => (byte)value.lng;
    }
    
    internal sealed class TypeInt16 : TypeLong
    {
        private TypeInt16() { }
        internal static readonly    TypeInt16 Instance = new TypeInt16();
        
        public    override  string  Name        => "short";
        public    override  Var     DefaultValue                           => new Var((short)default);
        public    override  Var     FromObject  (object obj)               => new Var((short)obj);
        public    override  object  ToObject    (in Var value)             => (short)value.lng;
    }
    
    internal sealed class TypeInt32 : TypeLong
    {
        private TypeInt32() { }
        internal static readonly    TypeInt32 Instance = new TypeInt32();
        
        public    override  string  Name        => "int";
        public    override  Var     DefaultValue                           => new Var((int)default);
        public    override  Var     FromObject  (object obj)               => new Var((int)obj);
        public    override  object  ToObject    (in Var value)             => (int)value.lng;
    }
    
    internal sealed class TypeInt64 : TypeLong
    {
        private TypeInt64() { }
        internal static readonly    TypeInt64 Instance = new TypeInt64();
        
        public    override  string  Name        => "long";
        public    override  Var     DefaultValue                           => new Var((long)default);
        public    override  Var     FromObject  (object obj)               => new Var((long)obj);
        public    override  object  ToObject    (in Var value)             => value.lng;
    }
    
    
    // --- nullable long (byte?, short?, int?,  long?) ---
    internal abstract class TypeNullableLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.lng.ToString();

    }

    internal sealed class TypeNullableInt8 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt8 Instance = new TypeNullableInt8();
        
        public    override  string  Name        => "byte?";
        public    override  Var     DefaultValue                           => new Var((byte?)null);
        public    override  Var     FromObject  (object obj)               => new Var((byte?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (byte?)value.lng : null;
    }
    
    internal sealed class TypeNullableInt16 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt16 Instance = new TypeNullableInt16();
        
        public    override  string  Name        => "short?";
        public    override  Var     DefaultValue                           => new Var((short?)null);
        public    override  Var     FromObject  (object obj)               => new Var((short?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (short?)value.lng : null;
    }
    
    internal sealed class TypeNullableInt32 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt32 Instance = new TypeNullableInt32();
        
        public    override  string  Name        => "int?";
        public    override  Var     DefaultValue                           => new Var((int?)null);
        public    override  Var     FromObject  (object obj)               => new Var((int?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (int?)value.lng : null;
    }
    
    internal sealed class TypeNullableInt64 : TypeNullableLong
    {
        internal static readonly    TypeNullableInt64 Instance = new TypeNullableInt64();
        
        public    override  string  Name        => "long?";
        public    override  Var     DefaultValue                           => new Var((long?)null);
        public    override  Var     FromObject  (object obj)               => new Var((long?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (long?) value.lng : null;
    }
    
    // --- float (32 bit) ---
    internal sealed class TypeFlt : VarType
    {
        internal static readonly    TypeFlt Instance = new TypeFlt();
        
        public    override  string  Name        => "float";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.Dbl == val2.Dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.Dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float)default);
        public    override  Var     FromObject  (object obj)               => new Var((float)obj);
        public    override  object  ToObject    (in Var value)             => (float)value.Dbl;
    }
    
    internal sealed class TypeNullableFlt : VarType
    {
        internal static readonly    TypeNullableFlt Instance = new TypeNullableFlt();
        
        public    override  string  Name        => "float?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.Dbl == val2.Dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.Dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float?)null);
        public    override  Var     FromObject  (object obj)               => new Var((float?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (float?)value.Dbl : null;
    }
    
    // --- double (64 bit) ---
    internal sealed class TypeDbl : VarType
    {
        internal static readonly    TypeDbl Instance = new TypeDbl();
        
        public    override  string  Name        => "double";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.Dbl == val2.Dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.Dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double)default);
        public    override  Var     FromObject  (object obj)               => new Var((double)obj);
        public    override  object  ToObject    (in Var value)             => value.Dbl;
    }
    
    internal sealed class TypeNullableDbl : VarType
    {
        internal static readonly    TypeNullableDbl Instance = new TypeNullableDbl();
        
        public    override  string  Name        => "double?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.Dbl == val2.Dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.Dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double?)null);
        public    override  Var     FromObject  (object obj)               => new Var((double?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (double?) value.Dbl : null;
    }
    
    // --- bool ---
    internal sealed class TypeBool : VarType
    {
        internal static readonly    TypeBool Instance = new TypeBool();
        
        public    override  string  Name        => "bool";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool)default);
        public    override  Var     FromObject  (object obj)               => new Var((bool)obj);
        public    override  object  ToObject    (in Var value)             => value.lng != 0;
    }
    
    internal sealed class TypeNullableBool : VarType
    {
        internal static readonly    TypeNullableBool Instance = new TypeNullableBool();
        
        public    override  string  Name        => "bool?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool?)null);
        public    override  Var     FromObject  (object obj)               => new Var((bool?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (bool?)(value.lng != 0) : null;
    }
    
    
    // --- char ---
    internal sealed class TypeChar : VarType
    {
        internal static readonly    TypeChar Instance = new TypeChar();
        
        public    override  string  Name        => "char";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => $"'{(char)value.lng}'";
        public    override  Var     DefaultValue                           => new Var((char)default);
        public    override  Var     FromObject  (object obj)               => new Var((char)obj);
        public    override  object  ToObject    (in Var value)             => (char)value.lng;
    }
    
    internal sealed class TypeNullableChar : VarType
    {
        internal static readonly    TypeNullableChar Instance = new TypeNullableChar();
        
        public    override  string  Name        => "char?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : $"'{(char)value.lng}'";
        public    override  Var     DefaultValue                           => new Var((char?)null);
        public    override  Var     FromObject  (object obj)               => new Var((char?)obj);
        public    override  object  ToObject    (in Var value)             => value.obj != null ? (char?)value.lng : null;
    }
}
}