// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;

// ReSharper disable once CheckNamespace
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack {

    static class Gen_Child
    {
        private const   long     _y          = 0x0000_0000_0000_0079;

        public static void ReadMsg (this ref MsgReader reader, ref Child obj) {
            if (!reader.ReadObject(out int len)) {
                obj = null;
                return;
            }
            if (obj == null) {
                obj = new Child();
            } else {
                obj.y = 0;
            }
            while (len-- > 0) {
                switch (reader.ReadKey()) {
                    case _y:    obj.y = reader.ReadInt32 (); continue;
                }
                reader.SkipTree();
            }
        }

        public static void WriteMsg(this ref MsgWriter writer, ref Child obj) {
            if (obj == null) {
                writer.WriteNull();
                return;
            }
            int map = writer.WriteMapFixBegin();
            int count = 1;
            writer.WriteKeyInt64   (1, _y, obj.y);
            writer.WriteMapFixEnd(map, count); // write element count
        }
    }
}