// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// Each <see cref="DB.UserAuth.Role"/> has a set of <see cref="DB.UserAuth.Role.rights"/>. <br/>
    /// Each <see cref="Right"/> is a rule used to grant or deny a specific database operation or command execution.<br/>
    /// The database operation or command execution is granted if any of it <see cref="DB.UserAuth.Role.rights"/>
    /// grant access.
    /// </summary>
    [Fri.Discriminator("type")] // , Description = "right type: allow, task, sendMessage, subscribeMessage, operation or predicate")]
    [Fri.Polymorph(typeof(RightAllow),              Discriminant = "allow")]
    [Fri.Polymorph(typeof(RightTask),               Discriminant = "task")]
    [Fri.Polymorph(typeof(RightSendMessage),        Discriminant = "sendMessage")]
    [Fri.Polymorph(typeof(RightSubscribeMessage),   Discriminant = "subscribeMessage")]
    [Fri.Polymorph(typeof(RightOperation),          Discriminant = "operation")]
    [Fri.Polymorph(typeof(RightPredicate),          Discriminant = "predicate")]
    public abstract class Right {
        /// <summary>optional description explaining the Right</summary>
        public              string      description;
        public  abstract    RightType   RightType { get; }

        public  abstract    Authorizer  ToAuthorizer();
    }
}