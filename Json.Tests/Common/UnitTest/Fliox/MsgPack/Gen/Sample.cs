// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo


// ReSharper disable once CheckNamespace

using System;
using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;

namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack {

    static class Gen_Sample
    {
        private const   int       _val          = 0;
        private const   ulong     _abcdefgh     = 0x6867_6665_6463_6261;
        private const   ulong     _x            = 0x0000_0000_0000_0078;
        private const   ulong     _child        = 0x0000_0064_6c69_6863;
        private static  byte[]    _x2           = new byte[] { (byte)'x' };

        public static void ReadMsg (ref Sample obj, ref MsgReader reader)
        {
            if (!reader.ReadObject(out int len)) {
                obj = null;
                return;
            }
            if (obj == null) {
                obj = new Sample();
            } else {
                obj.x = 0;
            }
            while (len-- > 0) {
                switch (reader.ReadKey()) {
                    // case _val:      obj.val = reader.ReadInt32 (); break;
                    case _x:        obj.x = reader.ReadInt32 ();                    continue;
                    case _child:    Gen_Child.ReadMsg(ref obj.child, ref reader);   continue;
                }
                reader.SkipTree(); break;
            }
        }
        
        public static void ReadMsgLongMembers (ref Sample obj, ref MsgReader reader)
        {
            if (!reader.ReadObject(out int len)) {
                obj = null;
                return;
            }
            if (obj == null) {
                obj = new Sample();
            } else {
                obj.x = 0;
            }
            while (len-- > 0) {
                switch (reader.ReadKey()) {
                    case _x:        if (reader.KeyName.SequenceEqual(_x2) ) { obj.x = reader.ReadInt32 (); continue; } break;
                    case _child:    Gen_Child.ReadMsg(ref obj.child, ref reader);           continue;
                }
                reader.SkipTree();
            }
        }

        public static void WriteMsg(ref Sample obj, ref MsgWriter writer)
        {
            if (obj == null) {
                writer.WriteNull();
                return;
            }
            int map = writer.WriteMapFix();
            int count = 1;
            // writer.WriteInt32   (_val, obj.val);
            writer.WriteInt32   (1, _x, obj.x);
            // writer.WriteInt32   (_x2, obj.x);
            if (writer.WriteMapKey (5, _child, obj.child != null, ref count)) {
                Gen_Child.WriteMsg  (ref obj.child, ref writer);
            }
            writer.WriteMapFixCount(map, count); // write element count
        }
    }
}