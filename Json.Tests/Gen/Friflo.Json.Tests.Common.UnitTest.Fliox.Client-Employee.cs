// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    static class Gen_Employee
    {
        private const int Gen_id = 0;
        private const int Gen_firstName = 1;
        private const int Gen_lastName = 2;

        private static bool ReadField (ref Employee obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_id:        obj.id        = reader.ReadString (field, obj.id,        out success);  return success;
                case Gen_firstName: obj.firstName = reader.ReadString (field, obj.firstName, out success);  return success;
                case Gen_lastName:  obj.lastName  = reader.ReadString (field, obj.lastName,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref Employee obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_id],        obj.id,        ref firstMember);
            writer.WriteString (fields[Gen_firstName], obj.firstName, ref firstMember);
            writer.WriteString (fields[Gen_lastName],  obj.lastName,  ref firstMember);
        }
    }
}

