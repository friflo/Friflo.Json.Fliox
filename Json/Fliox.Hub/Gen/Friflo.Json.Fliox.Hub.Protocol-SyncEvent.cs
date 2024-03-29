// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol
{
    static class Gen_SyncEvent
    {
        private const int Gen_usr = 0;
        private const int Gen_clt = 1;
        private const int Gen_db = 2;
        private const int Gen_tasks = 3;

        private static bool ReadField (ref SyncEvent obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_usr:   obj.usr   = reader.ReadShortString (field, obj.usr,   out success);  return success;
                case Gen_clt:   obj.clt   = reader.ReadShortString (field, obj.clt,   out success);  return success;
                case Gen_db:    obj.db    = reader.ReadShortString (field, obj.db,    out success);  return success;
                case Gen_tasks: obj.tasks = reader.ReadClass (field, obj.tasks, out success);  return success;
            }
            return false;
        }

        private static void Write(ref SyncEvent obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteShortString (fields[Gen_usr],   obj.usr,   ref firstMember);
            writer.WriteShortString (fields[Gen_clt],   obj.clt,   ref firstMember);
            writer.WriteShortString (fields[Gen_db],    obj.db,    ref firstMember);
            writer.WriteClass (fields[Gen_tasks], obj.tasks, ref firstMember);
        }
    }
}

