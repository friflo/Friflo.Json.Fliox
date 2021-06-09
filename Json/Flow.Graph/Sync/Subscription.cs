// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    public class Subscription
    {
        public List<ContainerFilter>    filters;
    }
    
    public class ContainerFilter
    {
        public string                   container;
        public TaskType[]               changes;
        public FilterOperation          filter;
    }

}