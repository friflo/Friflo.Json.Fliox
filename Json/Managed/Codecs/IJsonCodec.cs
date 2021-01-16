// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec
    {
        StubType    CreateStubType  (Type type);
        void        Write(JsonWriter writer, ref Slot slot, StubType stubType);
        bool        Read    (JsonReader reader, ref Slot slot, StubType stubType);
    }

    public enum SlotType {
        None,
        Double,
        Float,
        Long,
        Int,
        Short,
        Byte,
        Bool,
        Object,
    }

    public ref struct Slot {
        private object      obj;
        private long        lng;
        private double      dbl;
        
        public SlotType     Cat { get;  private set; }

        public object Obj {
            get => obj;
            set { obj = value; Cat = SlotType.Object; }
        }
        public double Dbl {
            get => dbl;
            set { dbl = value; Cat = SlotType.Double; }
        }
        public float Flt {
            get => (float)dbl;
            set { dbl = value; Cat = SlotType.Float; }
        }
        public long Lng {
            get => lng;
            set { lng = value; Cat = SlotType.Long; }
        }
        public int Int {
            get => (int)lng;
            set { lng = value; Cat = SlotType.Int; }
        }
        public short Short {
            get => (short)lng;
            set { lng = value; Cat = SlotType.Short; }
        }
        public byte Byte {
            get => (byte)lng;
            set { lng = value; Cat = SlotType.Byte; }
        }
        public bool Bool {
            get => lng != 0;
            set { lng = value ? 1 : 0; Cat = SlotType.Bool; }
        }
        
        public object Get () {
            switch (Cat) {
                case SlotType.None:     return null;
                case SlotType.Object:   return obj;
                //
                case SlotType.Double:   return dbl;
                case SlotType.Float:    return (float)dbl;
                //
                case SlotType.Long:     return lng;
                case SlotType.Int:      return (int)lng;
                case SlotType.Short:    return (short)lng;
                case SlotType.Byte:     return (byte)lng;
                //
                case SlotType.Bool:     return lng != 0;
            }
            return null; // unreachable
        }

        public void  Clear() {
            Cat = SlotType.None;
            obj = null;
            lng = 0;
            dbl = 0;
        }

        public override string ToString() {
            string val = null;
            switch (Cat) {
                case SlotType.None:     val = "None";       break;
                case SlotType.Object:   val = $"\"{obj}\""; break;
                //
                case SlotType.Double:   val = dbl.ToString(CultureInfo.InvariantCulture); break;
                case SlotType.Float:    val = dbl.ToString(CultureInfo.InvariantCulture); break;
                //
                case SlotType.Long:     val = lng.ToString(CultureInfo.InvariantCulture); break;
                case SlotType.Int:      val = lng.ToString(CultureInfo.InvariantCulture); break;
                case SlotType.Short:    val = lng.ToString(CultureInfo.InvariantCulture); break;
                case SlotType.Byte:     val = lng.ToString(CultureInfo.InvariantCulture); break;
                //
                case SlotType.Bool:     val = lng != 0 ? "true" : "false";              break;
            }
            
            return $"{val} ({Cat})";
        }
    }
}