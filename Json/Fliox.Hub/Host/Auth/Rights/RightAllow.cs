// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    public sealed class RightAllow : Right
    {
        public              string      database;
        public  override    RightType   RightType => RightType.allow;

        public  override    string      ToString() => "allow";

        public override Authorizer ToAuthorizer() {
            return new AuthorizeAllow(database);
        }
    }
}