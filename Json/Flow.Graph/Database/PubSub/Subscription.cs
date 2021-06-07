using System.Collections.Generic;
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
        public FilterOperation          filter;
    }

}