// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public abstract class VarType
    {
        public   abstract string    Name     { get; }
        internal abstract bool      IsNull   (in Var value);
        internal abstract bool      AreEqual (in Var val1, in Var val2);
        internal abstract string    AsString (in Var value);

        public   override string    ToString() => Name;

        public static VarType FromType(Type type) {
            if (type == typeof(bool))   return VarTypeBool.Instance;
            if (type == typeof(char))   return VarTypeChar.Instance;
            
            if (type == typeof(byte))   return VarTypeLong.Instance;
            if (type == typeof(short))  return VarTypeLong.Instance;
            if (type == typeof(int))    return VarTypeLong.Instance;
            if (type == typeof(long))   return VarTypeLong.Instance;
            
            if (type == typeof(float))  return VarTypeDbl.Instance;
            if (type == typeof(double)) return VarTypeDbl.Instance;
            
            // --- nullable
            if (type == typeof(bool?))  return VarTypeNullableBool.Instance;
            if (type == typeof(char?))  return VarTypeNullableChar.Instance;
            
            if (type == typeof(byte?))  return VarTypeNullableLong.Instance;
            if (type == typeof(short?)) return VarTypeNullableLong.Instance;
            if (type == typeof(int?))   return VarTypeNullableLong.Instance;
            if (type == typeof(long?))  return VarTypeNullableLong.Instance;
            
            if (type == typeof(float?)) return VarTypeNullableDbl.Instance;
            if (type == typeof(double?))return VarTypeNullableDbl.Instance;

            // --- reference type
            if (type == typeof(string)) return VarTypeString.Instance;

            return VarTypeObject.Instance;
        }
    }
    
    // --- object ---
    internal class VarTypeObject : VarType
    {
        internal static readonly    VarTypeObject Instance = new VarTypeObject();
        
        public    override  string  Name     => "object";
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj != null ? value.obj.ToString() : "null";
    }
    
    internal class VarTypeString : VarType
    {
        internal static readonly    VarTypeString Instance = new VarTypeString();
        
        public    override  string  Name     => "string";
        internal  override  bool    AreEqual (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj != null ? $"\"{(string)value.obj}\"" : "null";
    }
    
    // --- long (int64) ---
    internal class VarTypeLong : VarType
    {
        private VarTypeLong() { }
        internal static readonly    VarTypeLong Instance = new VarTypeLong();
        
        public    override  string  Name     => "long";
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => value.lng.ToString();
    }
    
    internal class VarTypeNullableLong : VarType
    {
        internal static readonly    VarTypeNullableLong Instance = new VarTypeNullableLong();
        
        public    override  string  Name     => "long?";
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? "null" : value.lng.ToString();
    }
    
    // --- double (64 bit) ---
    internal class VarTypeDbl : VarType
    {
        internal static readonly    VarTypeDbl Instance = new VarTypeDbl();
        
        public    override  string  Name     => "double";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.dbl == val2.dbl;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => value.dbl.ToString(CultureInfo.InvariantCulture);
    }
    
    internal class VarTypeNullableDbl : VarType
    {
        internal static readonly    VarTypeNullableDbl Instance = new VarTypeNullableDbl();
        
        public    override  string  Name     => "double?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.dbl == val2.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? "null" : value.dbl.ToString(CultureInfo.InvariantCulture);
    }
    
    // --- bool ---
    internal class VarTypeBool : VarType
    {
        internal static readonly    VarTypeBool Instance = new VarTypeBool();
        
        public    override  string  Name     => "bool";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => value.lng != 0 ? "true" : "false";
    }
    
    internal class VarTypeNullableBool : VarType
    {
        internal static readonly    VarTypeNullableBool Instance = new VarTypeNullableBool();
        
        public    override  string  Name     => "bool?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? "null" : value.lng != 0 ? "true" : "false";
    }
    
    // --- char ---
    internal class VarTypeChar : VarType
    {
        internal static readonly    VarTypeChar Instance = new VarTypeChar();
        
        public    override  string  Name     => "char";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => $"'{(char)value.lng}'";
    }
    
    internal class VarTypeNullableChar : VarType
    {
        internal static readonly    VarTypeNullableChar Instance = new VarTypeNullableChar();
        
        public    override  string  Name     => "char?";
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? "null" : $"'{(char)value.lng}'";
    }
}