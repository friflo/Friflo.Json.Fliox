// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.UserAuth;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.UserAuth
{
    static class Gen_Credentials
    {
        private const int Gen_userId = 0;
        private const int Gen_token = 1;

        private static bool ReadField (ref Credentials obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_userId: obj.userId = reader.ReadString (field, obj.userId, out success);  return success;
                case Gen_token:  obj.token  = reader.ReadString (field, obj.token,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref Credentials obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_userId], obj.userId, ref firstMember);
            writer.WriteString (fields[Gen_token],  obj.token,  ref firstMember);
        }
    }
}

