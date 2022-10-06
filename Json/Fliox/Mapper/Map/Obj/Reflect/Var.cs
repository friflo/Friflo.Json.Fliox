// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Friflo.Json.Fliox.Mapper.Map.Obj.Reflect
{
    internal readonly struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        internal  readonly  VarType type;
        internal  readonly  object  obj;
        internal  readonly  long    lng;
        internal  readonly  double  dbl;   // can merge with lng using BitConverter.DoubleToInt64Bits()
        
        internal            bool    IsEqual(in Var other)   =>  type.AreEqual(this, other);
        internal            bool    IsNull                  =>  type.IsNull(this);
        internal            bool    NotNull                 => !type.IsNull(this);
        internal            string  AsString()              =>  type.AsString(this);
        
        public    override  string  ToString()              =>  AsString();

        public static bool operator == (in Var val1, in Var val2) =>  val1.type.AreEqual(val1, val2);
        public static bool operator != (in Var val1, in Var val2) => !val1.type.AreEqual(val1, val2);
        
        internal Var (object value) {
            type    = VarTypeObject.Instance;
            obj     = value;
            lng     = 0;
            dbl     = 0;
        }
        
        internal Var (long value) {
            type    = VarTypeLong.Instance;
            obj     = NotNullTag;
            lng     = value;
            dbl     = 0;
        }

        internal Var (long? value) {
            type    = VarTypeNullableLong.Instance;
            obj     = value.HasValue ? NotNullTag : null;
            lng     = value ?? 0L;
            dbl     = 0;
        }
        
        internal Var (double value) {
            type    = VarTypeLong.Instance;
            obj     = NotNullTag;
            lng     = 0;
            dbl     = value;
        }

        internal Var (double? value) {
            type    = VarTypeNullableLong.Instance;
            obj     = value.HasValue ? NotNullTag : null;
            lng     = 0;
            dbl     = value ?? 0L;
        }
        
        private static readonly object NotNullTag = "not null";
    }
    
    internal abstract class VarType
    {
        internal abstract bool      IsNull   (in Var value);
        internal abstract bool      AreEqual (in Var val1, in Var val2);
        internal abstract string    AsString (in Var value);
    }
    
    internal class VarTypeObject : VarType
    {
        internal static readonly    VarTypeObject Instance = new VarTypeObject();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.ToString();
    }
    
    internal class VarTypeString : VarType
    {
        internal static readonly    VarTypeString Instance = new VarTypeString();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj.ToString();
    }
    
    internal class VarTypeLong : VarType
    {
        internal static readonly    VarTypeLong Instance = new VarTypeLong();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => value.lng.ToString();
    }
    
    internal class VarTypeNullableLong : VarType
    {
        internal static readonly    VarTypeNullableLong Instance = new VarTypeNullableLong();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.lng == val2.lng && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? null : value.lng.ToString();
    }
    
    internal class VarTypeDbl : VarType
    {
        internal static readonly    VarTypeDbl Instance = new VarTypeDbl();
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.dbl == val2.dbl;
        internal  override  bool    IsNull   (in Var value)             => false;
        internal  override  string  AsString (in Var value)             => value.dbl.ToString(CultureInfo.InvariantCulture);
    }
    
    internal class VarTypeNullableDbl : VarType
    {
        internal static readonly    VarTypeNullableDbl Instance = new VarTypeNullableDbl();
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        internal  override  bool    AreEqual (in Var val1, in Var val2) => val1.dbl == val2.dbl && val1.obj == val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj == null ? null : value.dbl.ToString(CultureInfo.InvariantCulture);
    }

    
    
}