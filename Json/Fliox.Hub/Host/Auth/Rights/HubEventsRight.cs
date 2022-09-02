using Friflo.Json.Fliox.Hub.DB.UserAuth;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    public class HubEventsRight : Right
    {
        public bool queueEvents;
        
        public override RightType RightType  => RightType.hubEvents;
        
        public override Authorizer ToAuthorizer() {
            return new AuthorizeEvents(queueEvents);
        }

        internal override void Validate(in RoleValidation validation) {
            throw new System.NotImplementedException();
        }
    }
}