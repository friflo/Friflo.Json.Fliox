// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.UserAuth;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.UserAuth
{
    static class Gen_AuthResult
    {
        private const int Gen_isValid = 0;

        private static bool ReadField (ref AuthResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_isValid: obj.isValid = reader.ReadBoolean (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref AuthResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteBoolean (fields[Gen_isValid], obj.isValid, ref firstMember);
        }
    }
}

