// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Fliox.MsgPack.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;

// ReSharper disable once CheckNamespace
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack {

    static class Gen_TestTypes
    {
        private const   long     _childL    = 0x0000_4164_6c69_6863;
        private const   long     _childA    = 0x0000_4c64_6c69_6863;
        private const   long     _intA      = 0x0000_0000_4174_6e69;
        private const   long     _intL      = 0x0000_0000_4c74_6e69;

        public static void ReadMsg (this ref MsgReader reader, ref TestTypes obj) {
            if (!reader.ReadObject(out int len)) {
                obj = null;
                return;
            }
            if (obj == null) {
                obj = new TestTypes();
            } else {
                obj.childL  = null;
                obj.childA  = null;
                obj.intA    = null;
                obj.intL    = null;
            }
            while (len-- > 0) {
                switch (reader.ReadKey()) {
                    case _childL:   reader.ReadMsg(ref obj.childL); continue;
                    case _childA:   reader.ReadMsg(ref obj.childA); continue;
                    case _intA:     reader.ReadMsg(ref obj.intA);   continue;
                    case _intL:     reader.ReadMsg(ref obj.intL);   continue;
                }
                reader.SkipTree();
            }
        }

        public static void WriteMsg(this ref MsgWriter writer, ref TestTypes obj) {
            if (obj == null) {
                writer.WriteNull();
                return;
            }
            int map = writer.WriteMapFixBegin();
            int count = 0;
            if (writer.AddKey(obj.childL != null)) {
                writer.WriteKey(6, _childL, ref count);
                writer.WriteMsg(ref obj.childL);
            }
            if (writer.AddKey(obj.childA != null)) {
                writer.WriteKey(6, _childA, ref count);
                writer.WriteMsg(ref obj.childA);
            }
            if (writer.AddKey(obj.intA != null)) {
                writer.WriteKey(4, _intA, ref count);
                writer.WriteMsg(ref obj.intA);
            }
            if (writer.AddKey(obj.intL != null)) {
                writer.WriteKey(4, _intL, ref count);
                writer.WriteMsg(ref obj.intL);
            }
            writer.WriteMapFixEnd(map, count); // write element count
        }
    }
}