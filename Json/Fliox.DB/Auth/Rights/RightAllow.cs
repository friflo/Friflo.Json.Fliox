// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.DB.Auth.Rights
{
    public class RightAllow : Right
    {
        public              bool        grant;
        public  override    RightType   RightType => RightType.allow;

        public  override    string      ToString() => grant.ToString();

        private  static readonly Authorizer Allow = new AuthorizeAllow();
        internal static readonly Authorizer Deny  = new AuthorizeDeny();
        
        public override Authorizer ToAuthorizer() {
            return grant ? Allow : Deny;
        }
    }
}