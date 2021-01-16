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
        bool        Read    (JsonReader reader, ref Slot slot, StubType stubType);
        void        Write   (JsonWriter writer, object obj, StubType stubType);
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
        private object   obj;
        private long     lng;
        private double   dbl;
        public  SlotType cat;
        
        public object Obj {
            get => obj;
            set { obj = value; cat = SlotType.Object; }
        }
        public double Dbl {
            get => dbl;
            set { dbl = value; cat = SlotType.Double; }
        }
        public float Flt {
            get => (float)dbl;
            set { dbl = value; cat = SlotType.Float; }
        }
        public long Lng {
            get => lng;
            set { lng = value; cat = SlotType.Long; }
        }
        public int Int {
            get => (int)lng;
            set { lng = value; cat = SlotType.Int; }
        }
        public short Short {
            get => (short)lng;
            set { lng = value; cat = SlotType.Short; }
        }
        public byte Byte {
            get => (byte)lng;
            set { lng = value; cat = SlotType.Byte; }
        }
        public bool Bool {
            get => lng != 0;
            set { lng = value ? 1 : 0; cat = SlotType.Bool; }
        }
        
        public object Get () {
            switch (cat) {
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
            cat = SlotType.None;
            obj = null;
            lng = 0;
            dbl = 0;
        }

        public override string ToString() {
            switch (cat) {
                case SlotType.None:     return "None";
                case SlotType.Object:   return $"\"{obj}\"";
                //
                case SlotType.Double:   return dbl.ToString(CultureInfo.InvariantCulture);
                case SlotType.Float:    return dbl.ToString(CultureInfo.InvariantCulture);
                //
                case SlotType.Long:     return lng.ToString(CultureInfo.InvariantCulture);
                case SlotType.Int:      return lng.ToString(CultureInfo.InvariantCulture);
                case SlotType.Short:    return lng.ToString(CultureInfo.InvariantCulture);
                case SlotType.Byte:     return lng.ToString(CultureInfo.InvariantCulture);
                //
                case SlotType.Bool:     return lng != 0 ? "true" : "false";
            }
            return "unreachable";
        }
    }
}