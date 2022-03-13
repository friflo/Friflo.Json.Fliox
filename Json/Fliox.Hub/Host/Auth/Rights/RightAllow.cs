// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Allow full access to the given <see cref="database"/>.<br/>
    /// In case <see cref="database"/> ends with a '*' e.g. 'test*' access to all databases with the prefix 'test'
    /// is granted.<br/>
    /// Using <see cref="database"/>: '*' grant access to all databases.
    /// </summary>
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