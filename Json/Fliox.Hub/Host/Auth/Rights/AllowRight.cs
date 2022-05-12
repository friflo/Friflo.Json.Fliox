// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Allow full access to the given <see cref="database"/>.<br/>
    /// </summary>
    public sealed class AllowRight : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Req]   public              string      database;
                public  override    RightType   RightType => RightType.allow;

        public  override            string      ToString() => "allow";

        public override Authorizer ToAuthorizer() {
            return new AuthorizeAllow(database);
        }
    }
}