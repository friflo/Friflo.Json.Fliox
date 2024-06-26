// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    static class Gen_TestKeyName
    {
        private const int Gen_testId = 0;
        private const int Gen_value = 1;

        private static bool ReadField (ref TestKeyName obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_testId: obj.testId = reader.ReadString (field, obj.testId, out success);  return success;
                case Gen_value:  obj.value  = reader.ReadString (field, obj.value,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref TestKeyName obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_testId], obj.testId, ref firstMember);
            writer.WriteString (fields[Gen_value],  obj.value,  ref firstMember);
        }
    }
}

