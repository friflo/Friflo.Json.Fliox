// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Auth.Rights
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(RightAllow),              Discriminant = "allow")]
    [Fri.Polymorph(typeof(RightTask),               Discriminant = "task")]
    [Fri.Polymorph(typeof(RightMessage),            Discriminant = "message")]
    [Fri.Polymorph(typeof(RightSubscribeMessage),   Discriminant = "subscribeMessage")]
    [Fri.Polymorph(typeof(RightDatabase),           Discriminant = "database")]
    [Fri.Polymorph(typeof(RightPredicate),          Discriminant = "predicate")]
    public abstract class Right {
        public          string          description;
        public abstract RightType       RightType { get; }

        public abstract Authorizer      ToAuthorizer();
    }
}