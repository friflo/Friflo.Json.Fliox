using System;

namespace Friflo.Json.Tests.Common.UnitTest.EntityGraph.Api
{

    public enum Order
    {
        None,
        Asc,
        Desc,
    }

    public static class Graph
    {
        public static TSource Query<TSource, TOrderBy>(
            int                     limit   = 0,
            Func<TSource, TOrderBy> orderBy = null,
            Order                   order   = Order.None,
            Func<TSource, bool>     where   = null,
            Func<TSource>           select  = null
        ) where TSource : class
        {
            return null;
        }
    }

}