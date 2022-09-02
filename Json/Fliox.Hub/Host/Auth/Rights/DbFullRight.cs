// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.DB.UserAuth;

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Allow full access to the given <see cref="database"/>.<br/>
    /// </summary>
    public sealed class DbFullRight : Right
    {
        /// <summary>a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*'</summary>
        [Required]  public              string      database;
                    public  override    RightType   RightType => RightType.dbFull;

        public  override                string      ToString() => "dbFull";

        public override Authorizer ToAuthorizer() {
            return new AuthorizeDatabase(database);
        }
        
        internal override void Validate(in RoleValidation validation) {
            validation.ValidateDatabase(this, database);
        }
    }
}