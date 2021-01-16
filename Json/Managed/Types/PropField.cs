// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
{

    public abstract class PropField : IDisposable
    {
        internal readonly   String          name;
        internal readonly   SimpleType.Id   type;
        public   readonly   SlotType        slotType;
        public              StubType        FieldType { get; internal set; }    // never null 
        internal readonly   Type            fieldTypeNative;                    // never null 
        private  readonly   ClassType       declType;
        internal            Bytes           nameBytes;
        internal            ConstructorInfo collectionConstructor;

        internal PropField (ClassType declType, String name, SimpleType.Id type,  SlotType slotType, Type fieldType)
        {
            this.declType               = declType;
            this.name                   = name;
            this.nameBytes              = new Bytes(name);
            this.type                   = type;
            this.fieldTypeNative        = fieldType;
            this.slotType               = slotType;
            if (fieldType == null)
                throw new InvalidOperationException("Expect fieldType non null");
        }

        public void Dispose() {
            nameBytes.Dispose();
        }

        public void AppendName(ref Bytes bb)
        {
            bb.AppendBytes(ref nameBytes);
        }

        public String GetString (Object prop)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     return InternalGetString    (prop);
                case SimpleType.Id. Long:       return InternalGetLong      (prop) .ToString(NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Integer:    return InternalGetInt       (prop) .ToString(NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Short:      return InternalGetShort     (prop) .ToString(NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Byte:       return InternalGetByte      (prop) .ToString(NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Bool:       return InternalGetBool      (prop) ? "true"  : "false";
                case SimpleType.Id. Double:     return InternalGetDouble    (prop) .ToString(NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Float:      return InternalGetFloat     (prop) .ToString(NumberFormatInfo.InvariantInfo);
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }
        /*
        public void SetString (Object prop, String val)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     InternalSetString   (prop, val) ;                   break;
                case SimpleType.Id. Long:       InternalSetLong     (prop, long.    Parse (val,NumberFormatInfo.InvariantInfo) );   break;
                case SimpleType.Id. Integer:    InternalSetInt      (prop, int.     Parse (val, NumberFormatInfo.InvariantInfo) );  break;
                case SimpleType.Id. Short:      InternalSetShort    (prop, short.   Parse (val, NumberFormatInfo.InvariantInfo) );  break;
                case SimpleType.Id. Byte:       InternalSetByte     (prop, byte.    Parse (val, NumberFormatInfo.InvariantInfo) );  break;
                case SimpleType.Id. Bool:       InternalSetBool     (prop, bool.    Parse (val) );                                  break;
                case SimpleType.Id. Double:     InternalSetDouble   (prop, double.  Parse (val, NumberFormatInfo.InvariantInfo) );  break;
                case SimpleType.Id. Float:      InternalSetFloat    (prop, float.   Parse (val, NumberFormatInfo.InvariantInfo) );  break;
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        } */
        
        public Object GetObject (Object prop)
        {
            try
            {
                if (type == SimpleType. Id.Object)
                    return InternalGetObject    (prop) ;
                throw new FrifloException("unhandled case for field: " + name + " type: " + type);
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }

        public void SetObject (object prop, Object val)
        {
            InternalSetObject(prop, val);
        }
        
        public void SetField (object prop, ref Slot val)
        {
            switch (val.Cat) {
                case SlotType.Object:   InternalSetObject   (prop, val.Obj);    return;
                //
                case SlotType.Double:   InternalSetDouble   (prop, val.Dbl);    return;
                case SlotType.Float:    InternalSetFloat    (prop, val.Flt);    return;
                //
                case SlotType.Long:     InternalSetLong     (prop, val.Lng);    return;
                case SlotType.Int:      InternalSetInt      (prop, val.Int);    return;
                case SlotType.Short:    InternalSetShort    (prop, val.Short);  return;
                case SlotType.Byte:     InternalSetByte     (prop, val.Byte);   return;
                //
                case SlotType.Bool:     InternalSetBool     (prop, val.Bool);   return;
            }
        }
        
        public void GetField (object prop, ref Slot val)
        {
            switch (slotType) {
                case SlotType.Object:   val.Obj     = InternalGetObject   (prop);    return;
                //
                case SlotType.Double:   val.Dbl     = InternalGetDouble   (prop);    return;
                case SlotType.Float:    val.Flt     = InternalGetFloat    (prop);    return;
                //
                case SlotType.Long:     val.Lng     = InternalGetLong     (prop);    return;
                case SlotType.Int:      val.Int     = InternalGetInt      (prop);    return;
                case SlotType.Short:    val.Short   = InternalGetShort    (prop);    return;
                case SlotType.Byte:     val.Byte    = InternalGetByte     (prop);    return;
                //
                case SlotType.Bool:     val.Bool    = InternalGetBool     (prop);    return;
            }
        }

        public long GetLong (Object prop)
        {
            if (type == SimpleType.Id.Long)
            {
                try
                {
                    return InternalGetLong (prop);
                }
                catch (Exception e)
                {
                    throw new FrifloException("Set field failed. field: " + name, e);
                }
            }
            else
                return GetInt (prop);       
        }
        /*
        public void SetLong (Object prop, long val)
        {
            if (type == SimpleType.Id.Long)
            {
                try
                {
                    InternalSetLong (prop, val);
                }
                catch (Exception e)
                {
                    throw new FrifloException("Set field failed. field: " + name, e);
                }
            }
            else
                SetInt (prop, (int)val);    
        } */

        public int GetInt (Object prop)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:
                    String str =                                InternalGetString   (prop) ;
                    throw new FrifloException("No conversion to bool. field: " + name + " val: " + str);
                case SimpleType.Id. Long:       return (int)    InternalGetLong     (prop) ;
                case SimpleType.Id. Integer:    return          InternalGetInt      (prop) ;
                case SimpleType.Id. Short:      return          InternalGetShort    (prop) ;
                case SimpleType.Id. Byte:       return          InternalGetByte     (prop) ;
                case SimpleType.Id. Bool:       return          InternalGetBool     (prop) ? 1 : 0;
                case SimpleType.Id. Double:     return (int)    InternalGetDouble   (prop) ;
                case SimpleType.Id. Float:      return (int)    InternalGetFloat    (prop) ;
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }
        /*
        public void SetInt (Object prop, int val)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     InternalSetString   (prop, val .ToString(NumberFormatInfo.InvariantInfo));      break;
                case SimpleType.Id. Long:       InternalSetLong     (prop,          val);           break;
                case SimpleType.Id. Integer:    InternalSetInt      (prop,          val);           break;
                case SimpleType.Id. Short:      InternalSetShort    (prop, (short)  val);           break;
                case SimpleType.Id. Byte:       InternalSetByte     (prop, (byte)   val);           break;
                case SimpleType.Id. Bool:       InternalSetBool     (prop,          val != 0 );     break; 
                case SimpleType.Id. Double:     InternalSetDouble   (prop,          val);           break;
                case SimpleType.Id. Float:      InternalSetFloat    (prop,          val);           break;
                default:                        throw new FrifloException ("no conversion to int. type: " + type);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        } */

        public double GetDouble (Object prop)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     return Double.Parse ( InternalGetString(prop) ,  NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Long:       return InternalGetLong      (prop) ;
                case SimpleType.Id. Integer:    return InternalGetInt       (prop) ;
                case SimpleType.Id. Short:      return InternalGetShort     (prop) ;
                case SimpleType.Id. Byte:       return InternalGetByte      (prop) ;
                case SimpleType.Id. Bool:       return InternalGetBool      (prop) ? 1 : 0;
                case SimpleType.Id. Double:     return InternalGetDouble    (prop) ;
                case SimpleType.Id. Float:      return InternalGetFloat     (prop) ;
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }
        /*
        public bool SetNumber (ref JsonParser parser, Object prop) {
            try
            {
                bool success;
                switch (type)
                {
                case SimpleType.Id. String:     InternalSetString   (prop, parser.value.ToString());
                    return true;
                case SimpleType.Id. Long:       InternalSetLong     (prop, parser.ValueAsLong   (out success)); break;
                case SimpleType.Id. Integer:    InternalSetInt      (prop, parser.ValueAsInt    (out success)); break;
                case SimpleType.Id. Short:      InternalSetShort    (prop, parser.ValueAsShort  (out success)); break;
                case SimpleType.Id. Byte:       InternalSetByte     (prop, parser.ValueAsByte   (out success)); break;
                case SimpleType.Id. Bool:      
                    parser.Error("PropField", $"Field is not a number type. Field type: {type}");
                    return false; 
                case SimpleType.Id. Double:     InternalSetDouble   (prop, parser.ValueAsDouble (out success)); break;
                case SimpleType.Id. Float:      InternalSetFloat    (prop, parser.ValueAsFloat  (out success)); break;
                default:                        throw new FrifloException ("no conversion to double. type: " + type);
                }
                return success;
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        } */

        public float GetFloat (Object prop)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     return Single.Parse ( InternalGetString(prop) ,  NumberFormatInfo.InvariantInfo);
                case SimpleType.Id. Long:       return InternalGetLong              (prop) ;
                case SimpleType.Id. Integer:    return InternalGetInt               (prop) ;
                case SimpleType.Id. Short:      return InternalGetShort             (prop) ;
                case SimpleType.Id. Byte:       return InternalGetByte              (prop) ;
                case SimpleType.Id. Bool:       return InternalGetBool              (prop) ? 1 : 0;
                case SimpleType.Id. Double:     return (float) InternalGetDouble    (prop) ;
                case SimpleType.Id. Float:      return InternalGetFloat             (prop) ;
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }
        /*
        public void SetFloat (Object prop, float val)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     InternalSetString   (prop, val .ToString(NumberFormatInfo.InvariantInfo));  break;
                case SimpleType.Id. Long:       InternalSetLong     (prop, (long)   val);           break;
                case SimpleType.Id. Integer:    InternalSetInt      (prop, (int)    val);           break;
                case SimpleType.Id. Short:      InternalSetShort    (prop, (short)  val);           break;
                case SimpleType.Id. Byte:       InternalSetByte     (prop, (byte)   val);           break;
                case SimpleType.Id. Bool:       InternalSetBool     (prop,          val != 0 );     break;
                case SimpleType.Id. Double:     InternalSetDouble   (prop,          val);           break;
                case SimpleType.Id. Float:      InternalSetFloat    (prop,          val);           break;
                default:        throw new FrifloException ("no conversion to double. type: " + type);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }*/

        public bool GetBool (Object prop)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:
                    String str =                       InternalGetString    (prop) ;
                    if (str. Equals ("true"))   return true;
                    if (str. Equals ("false"))  return false;
                    throw new FrifloException("No conversion to bool. field: " + name + " val: " + str);
                case SimpleType.Id. Long:       return InternalGetLong      (prop) != 0;
                case SimpleType.Id. Integer:    return InternalGetInt       (prop) != 0;
                case SimpleType.Id. Short:      return InternalGetShort     (prop) != 0;
                case SimpleType.Id. Byte:       return InternalGetByte      (prop) != 0;
                case SimpleType.Id. Bool:       return InternalGetBool      (prop);
                case SimpleType.Id. Double:     return InternalGetDouble    (prop) != 0;
                case SimpleType.Id. Float:      return InternalGetFloat     (prop) != 0;
                default:
                    throw new FrifloException("unhandled case for field: " + name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }

        public void SetBool (Object prop, bool val)
        {
            try
            {
                switch (type)
                {
                case SimpleType.Id. String:     InternalSetString   (prop, val ? "true" : "false");     break;
                case SimpleType.Id. Long:       InternalSetLong     (prop,          (val ? 1 : 0));     break;
                case SimpleType.Id. Integer:    InternalSetInt      (prop,          (val ? 1 : 0));     break;
                case SimpleType.Id. Short:      InternalSetShort    (prop, (short)  (val ? 1 : 0));     break;
                case SimpleType.Id. Byte:       InternalSetByte     (prop, (byte)   (val ? 1 : 0));     break;
                case SimpleType.Id. Bool:       InternalSetBool     (prop,           val         );     break;
                case SimpleType.Id. Double:     InternalSetDouble   (prop,          (val ? 1 : 0));     break;
                case SimpleType.Id. Float:      InternalSetFloat    (prop,          (val ? 1 : 0));     break;
                default:                        throw new FrifloException ("no conversion to bool. type: " + type);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("Set field failed. field: " + name, e);
            }
        }

        private FrifloException Except()
        {
            return new FrifloException("member doesnt support Get/Set");
        }
        

        public abstract bool IsAssignable();
        
        internal    virtual Object  InternalGetObject   (Object obj)    { throw Except(); }
        internal    virtual String  InternalGetString   (Object obj)    { throw Except(); }
        internal    virtual long    InternalGetLong     (Object obj)    { throw Except(); }
        internal    virtual int     InternalGetInt      (Object obj)    { throw Except(); }
        internal    virtual short   InternalGetShort    (Object obj)    { throw Except(); }
        internal    virtual byte    InternalGetByte     (Object obj)    { throw Except(); }
        internal    virtual bool    InternalGetBool     (Object obj)    { throw Except(); }
        internal    virtual double  InternalGetDouble   (Object obj)    { throw Except(); }
        internal    virtual float   InternalGetFloat    (Object obj)    { throw Except(); }
        
        internal    virtual void    InternalSetObject   (Object obj, Object val)    { throw Except(); }
        internal    virtual void    InternalSetString   (Object obj, String val)    { throw Except(); }
        internal    virtual void    InternalSetLong     (Object obj, long val)      { throw Except(); }
        internal    virtual void    InternalSetInt      (Object obj, int val)       { throw Except(); }
        internal    virtual void    InternalSetShort    (Object obj, short val)     { throw Except(); }
        internal    virtual void    InternalSetByte     (Object obj, byte val)      { throw Except(); }
        internal    virtual void    InternalSetBool     (Object obj, bool val)      { throw Except(); }
        internal    virtual void    InternalSetDouble   (Object obj, double val)    { throw Except(); }
        internal    virtual void    InternalSetFloat    (Object obj, float val)     { throw Except(); }

        //
    }
}
