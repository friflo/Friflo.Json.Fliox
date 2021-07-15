// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Auth
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(RightAllow),      Discriminant = "allow")]
    [Fri.Polymorph(typeof(RightTasks),      Discriminant = "tasks")]
    [Fri.Polymorph(typeof(RightMessages),   Discriminant = "messages")]
    [Fri.Polymorph(typeof(RightContainers), Discriminant = "containers")]
    public abstract class Right {
        
        public abstract RightType       RightType { get; }
    }
    
    public class RightAllow : Right
    {
        public          bool                    allow;
        public override RightType               RightType => RightType.allow;
    }
    
    public class RightTasks : Right
    {
        public          List<string>            tasks;
        public override RightType               RightType => RightType.tasks;
    }
    
    public class RightMessages : Right
    {
        public          List<string>            messages;
        public override RightType               RightType => RightType.messages;
    }
    
    public class RightContainers : Right
    {
        public          List<ContainerAccess>   containers;
        public override RightType               RightType => RightType.containers;
    }
    
    public class ContainerAccess
    {
        public          List<string>            access;
    }

    // ReSharper disable InconsistentNaming
    public enum RightType {
        allow,
        tasks,
        messages,
        containers,
    }
}