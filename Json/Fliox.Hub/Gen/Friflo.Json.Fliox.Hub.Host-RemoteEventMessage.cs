// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Host
{
    static class Gen_RemoteEventMessage
    {
        private const int Gen_msg = 0;
        private const int Gen_clt = 1;
        private const int Gen_seq = 2;
        private const int Gen_ev = 3;

        private static bool ReadField (ref RemoteEventMessage obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_msg: obj.msg = reader.ReadString (field, obj.msg, out success);  return success;
                case Gen_clt: obj.clt = reader.ReadShortString (field, obj.clt, out success);  return success;
                case Gen_seq: obj.seq = reader.ReadInt32 (field, out success);  return success;
                case Gen_ev:  obj.ev  = reader.ReadClass (field, obj.ev,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref RemoteEventMessage obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_msg], obj.msg, ref firstMember);
            writer.WriteShortString (fields[Gen_clt], obj.clt, ref firstMember);
            writer.WriteInt32 (fields[Gen_seq], obj.seq, ref firstMember);
            writer.WriteClass (fields[Gen_ev],  obj.ev,  ref firstMember);
        }
    }
}

