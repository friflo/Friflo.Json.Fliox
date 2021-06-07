// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database.PubSub
{
    public class Subscription
    {
        public List<ContainerFilter>    filters;
    }
    
    public class ContainerFilter
    {
        public string                   container;
        public TaskType[]               taskTypes;
        public FilterOperation          filter;
    }

}