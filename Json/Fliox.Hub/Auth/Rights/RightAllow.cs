// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Auth.Rights
{
    public sealed class RightAllow : Right
    {
        public              bool        grant;
        public  override    RightType   RightType => RightType.allow;

        public  override    string      ToString() => grant.ToString();

        
        public override Authorizer ToAuthorizer() {
            if (grant)
                return new AuthorizeAllow(database);
            return new AuthorizeDeny(database);
        }
    }
}