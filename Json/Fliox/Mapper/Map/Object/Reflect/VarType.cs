// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    internal abstract class VarType
    {
        internal abstract bool      IsNull   (in Var value);
        internal abstract bool      AreEqual (in Var val1, in Var val2);
        internal abstract string    AsString (in Var value);
    }
    
    // --- object ---
    internal class VarTypeObject : VarType
    {
        internal static readonly    VarTypeObject Instance = new VarTypeObject();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => ReferenceEquals(val1.obj, val2.obj);
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj.ToString();
    }
    
    internal class VarTypeString : VarType
    {
        internal static readonly    VarTypeString Instance = new VarTypeString();
        
        internal  override  bool    AreEqual (in Var val1, in Var val2) => (string)val1.obj == (string)val2.obj;
        internal  override  bool    IsNull   (in Var value)             => value.obj == null;
        internal  override  string  AsString (in Var value)             => value.obj.ToString();
    }
    
    // --- long (int64) ---
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
    
    // --- double (64 bit) ---
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