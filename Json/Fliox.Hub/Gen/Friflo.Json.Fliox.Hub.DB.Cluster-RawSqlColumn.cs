// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_RawSqlColumn
    {
        private const int Gen_name = 0;
        private const int Gen_type = 1;

        private static bool ReadField (ref RawSqlColumn obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_name: obj.name = reader.ReadString (field, obj.name, out success);  return success;
                case Gen_type: obj.type = reader.ReadEnum (field, obj.type, out success);  return success;
            }
            return false;
        }

        private static void Write(ref RawSqlColumn obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_name], obj.name, ref firstMember);
            writer.WriteEnum (fields[Gen_type], obj.type, ref firstMember);
        }
    }
}

