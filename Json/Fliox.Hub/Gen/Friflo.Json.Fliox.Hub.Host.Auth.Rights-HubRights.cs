// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    static class Gen_HubRights
    {
        private const int Gen_queueEvents = 0;

        private static bool ReadField (ref HubRights obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_queueEvents: obj.queueEvents = reader.ReadBooleanNull (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref HubRights obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteBooleanNull (fields[Gen_queueEvents], obj.queueEvents, ref firstMember);
        }
    }
}

