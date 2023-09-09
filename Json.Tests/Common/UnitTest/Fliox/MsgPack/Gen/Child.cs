// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;

// ReSharper disable once CheckNamespace
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack {

    static class Gen_Child
    {
        private const   ulong     _y          = 0x0000_0000_0000_0079;

        public static void ReadMsg (ref Child obj, ref MsgReader reader) {
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
                    // case _val:      obj.val = reader.ReadInt32 (); break;
                    case _y:    obj.y = reader.ReadInt32 (); continue;
                }
                reader.SkipTree(); break;
            }
        }

        public static void WriteMsg(ref Child obj, ref MsgWriter writer) {
            if (obj == null) {
                writer.WriteNull();
                return;
            }
            int map = writer.WriteMapFix();
            int count = 1;
            // writer.WriteInt32   (_val, obj.val);
            writer.WriteKeyInt32   (1, _y, obj.y);
            // writer.WriteInt32   (_x2, obj.x);
            writer.WriteMapFixCount(map, count); // write element count
        }
    }
}