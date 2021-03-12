using System;

namespace Friflo.Json.Tests.Common.UnitTest.EntityGraph.Api
{

    public enum Order
    {
        Ascending,
        Descending,
    }

    public static class Graph
    {
        public static TSource Query<TSource, TOrderBy>(
            int                     limit   = 0,
            Func<TSource, TOrderBy> orderBy = null,
            Order                   order   = Order.Ascending,
            Func<TSource, bool>     where   = null,
            Func<TSource>           select  = null
        ) where TSource : class
        {
            return null;
        }
    }

}