// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using static Friflo.Json.Fliox.Mapper.Map.Object.Reflect.Var;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public abstract class VarType
    {
        public   abstract string    Name        { get; }
        internal abstract bool      IsNull      (in Var value);
        internal abstract bool      AreEqual    (in Var val1, in Var val2);
        internal abstract string    AsString    (in Var value);
        public   abstract Var       DefaultValue{ get; }
        public   abstract Var       FromObject  (object obj);

        public   override string    ToString() => Name;

        /// <summary> Method has many conditions. Cache returned VarType in case using frequently </summary>
        public static VarType FromType(Type type) {
            if (type == typeof(bool))   return VarTypeBool.Instance;
            if (type == typeof(char))   return VarTypeChar.Instance;
            
            if (type == typeof(byte))   return VarTypeInt8.Instance;
            if (type == typeof(short))  return VarTypeInt16.Instance;
            if (type == typeof(int))    return VarTypeInt32.Instance;
            if (type == typeof(long))   return VarTypeInt64.Instance;
            
            if (type == typeof(float))  return VarTypeFlt.Instance;
            if (type == typeof(double)) return VarTypeDbl.Instance;
            
            // --- nullable
            if (type == typeof(bool?))  return VarTypeNullableBool.Instance;
            if (type == typeof(char?))  return VarTypeNullableChar.Instance;
            
            if (type == typeof(byte?))  return VarTypeNullableInt8.Instance;
            if (type == typeof(short?)) return VarTypeNullableInt16.Instance;
            if (type == typeof(int?))   return VarTypeNullableInt32.Instance;
            if (type == typeof(long?))  return VarTypeNullableInt64.Instance;
            
            if (type == typeof(float?)) return VarTypeNullableFlt.Instance;
            if (type == typeof(double?))return VarTypeNullableDbl.Instance;

            // --- reference type
            if (type == typeof(string)) return VarTypeString.Instance;

            return VarTypeObject.Instance;
        }
    }
    
//  ------------------------------------- VarType implementations -------------------------------------
/// Nest concrete VarType classes in Var to make all <see cref="Var"/> fields private
public partial struct Var {
        
    // --- object ---
    internal sealed class VarTypeObject : VarType
    {
        internal static readonly    VarTypeObject Instance = new VarTypeObject();
        
        public    override  string  Name     => "object";
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj != null ? value.obj.ToString() : "null";
        public    override  Var     DefaultValue                           => new Var((object)null);
        public    override  Var     FromObject  (object obj)               => new Var(obj);
    }
    
    internal sealed class VarTypeString : VarType
    {
        internal static readonly    VarTypeString Instance = new VarTypeString();
        
        public    override  string  Name     => "string";
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj != null ? $"\"{(string)value.obj}\"" : "null";
        public    override  Var     DefaultValue                           => new Var((string)null);
        public    override  Var     FromObject  (object obj)               => new Var((string)obj);
    }
    
    
    // --- long (byte, short, int, long) ---
    internal abstract class VarTypeLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.lng.ToString();
    }
    
    internal sealed class VarTypeInt8 : VarTypeLong
    {
        private VarTypeInt8() { }
        internal static readonly    VarTypeInt8 Instance = new VarTypeInt8();
        
        public    override  string  Name        => "byte";
        public    override  Var     DefaultValue                           => new Var((byte)default);
        public    override  Var     FromObject  (object obj)               => new Var((byte)obj);
    }
    
    internal sealed class VarTypeInt16 : VarTypeLong
    {
        private VarTypeInt16() { }
        internal static readonly    VarTypeInt16 Instance = new VarTypeInt16();
        
        public    override  string  Name        => "short";
        public    override  Var     DefaultValue                           => new Var((short)default);
        public    override  Var     FromObject  (object obj)               => new Var((short)obj);
    }
    
    internal sealed class VarTypeInt32 : VarTypeLong
    {
        private VarTypeInt32() { }
        internal static readonly    VarTypeInt32 Instance = new VarTypeInt32();
        
        public    override  string  Name        => "int";
        public    override  Var     DefaultValue                           => new Var((int)default);
        public    override  Var     FromObject  (object obj)               => new Var((int)obj);
    }
    
    internal sealed class VarTypeInt64 : VarTypeLong
    {
        private VarTypeInt64() { }
        internal static readonly    VarTypeInt64 Instance = new VarTypeInt64();
        
        public    override  string  Name        => "long";
        public    override  Var     DefaultValue                           => new Var((long)default);
        public    override  Var     FromObject  (object obj)               => new Var((long)obj);
    }
    
    
    // --- nullable long (byte?, short?, int?,  long?) ---
    internal abstract class VarTypeNullableLong : VarType
    {
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.lng.ToString();

    }

    internal sealed class VarTypeNullableInt8 : VarTypeNullableLong
    {
        internal static readonly    VarTypeNullableInt8 Instance = new VarTypeNullableInt8();
        
        public    override  string  Name        => "byte?";
        public    override  Var     DefaultValue                           => new Var((byte?)null);
        public    override  Var     FromObject  (object obj)               => new Var((byte?)obj);
    }
    
    internal sealed class VarTypeNullableInt16 : VarTypeNullableLong
    {
        internal static readonly    VarTypeNullableInt16 Instance = new VarTypeNullableInt16();
        
        public    override  string  Name        => "short?";
        public    override  Var     DefaultValue                           => new Var((short?)null);
        public    override  Var     FromObject  (object obj)               => new Var((short?)obj);
    }
    
    internal sealed class VarTypeNullableInt32 : VarTypeNullableLong
    {
        internal static readonly    VarTypeNullableInt32 Instance = new VarTypeNullableInt32();
        
        public    override  string  Name        => "int?";
        public    override  Var     DefaultValue                           => new Var((int?)null);
        public    override  Var     FromObject  (object obj)               => new Var((int?)obj);
    }
    
    internal sealed class VarTypeNullableInt64 : VarTypeNullableLong
    {
        internal static readonly    VarTypeNullableInt64 Instance = new VarTypeNullableInt64();
        
        public    override  string  Name        => "long?";
        public    override  Var     DefaultValue                           => new Var((long?)null);
        public    override  Var     FromObject  (object obj)               => new Var((long?)obj);
    }
    
    // --- float (32 bit) ---
    internal sealed class VarTypeFlt : VarType
    {
        internal static readonly    VarTypeFlt Instance = new VarTypeFlt();
        
        public    override  string  Name        => "float";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.dbl == val2.dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float)default);
        public    override  Var     FromObject  (object obj)               => new Var((float)obj);
    }
    
    internal sealed class VarTypeNullableFlt : VarType
    {
        internal static readonly    VarTypeNullableFlt Instance = new VarTypeNullableFlt();
        
        public    override  string  Name        => "float?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.dbl == val2.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((float?)null);
        public    override  Var     FromObject  (object obj)               => new Var((float?)obj);
    }
    
    // --- double (64 bit) ---
    internal sealed class VarTypeDbl : VarType
    {
        internal static readonly    VarTypeDbl Instance = new VarTypeDbl();
        
        public    override  string  Name        => "double";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.dbl == val2.dbl;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double)default);
        public    override  Var     FromObject  (object obj)               => new Var((double)obj);
    }
    
    internal sealed class VarTypeNullableDbl : VarType
    {
        internal static readonly    VarTypeNullableDbl Instance = new VarTypeNullableDbl();
        
        public    override  string  Name        => "double?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.dbl == val2.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.dbl.ToString(CultureInfo.InvariantCulture);
        public    override  Var     DefaultValue                           => new Var((double?)null);
        public    override  Var     FromObject  (object obj)               => new Var((double?)obj);
    }
    
    // --- bool ---
    internal sealed class VarTypeBool : VarType
    {
        internal static readonly    VarTypeBool Instance = new VarTypeBool();
        
        public    override  string  Name        => "bool";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => value.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool)default);
        public    override  Var     FromObject  (object obj)               => new Var((bool)obj);
    }
    
    internal sealed class VarTypeNullableBool : VarType
    {
        internal static readonly    VarTypeNullableBool Instance = new VarTypeNullableBool();
        
        public    override  string  Name        => "bool?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : value.lng != 0 ? "true" : "false";
        public    override  Var     DefaultValue                           => new Var((bool?)null);
        public    override  Var     FromObject  (object obj)               => new Var((bool?)obj);
    }
    
    
    // --- char ---
    internal sealed class VarTypeChar : VarType
    {
        internal static readonly    VarTypeChar Instance = new VarTypeChar();
        
        public    override  string  Name        => "char";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull      (in Var value)             => false;
        internal  override  string  AsString    (in Var value)             => $"'{(char)value.lng}'";
        public    override  Var     DefaultValue                           => new Var((char)default);
        public    override  Var     FromObject  (object obj)               => new Var((char)obj);
    }
    
    internal sealed class VarTypeNullableChar : VarType
    {
        internal static readonly    VarTypeNullableChar Instance = new VarTypeNullableChar();
        
        public    override  string  Name        => "char?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual    (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull      (in Var value)             => value.obj == null;
        internal  override  string  AsString    (in Var value)             => value.obj == null ? "null" : $"'{(char)value.lng}'";
        public    override  Var     DefaultValue                           => new Var((char?)null);
        public    override  Var     FromObject  (object obj)               => new Var((char?)obj);
    }
}
}