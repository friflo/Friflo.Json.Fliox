using System;
using System.Linq.Expressions;
using FluentAssertions;
using Friflo.Json.EntityGraph;

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
        
        public static void Update<TEntity, TValue>(TEntity entity, Func<TEntity, TValue> field) where TEntity : Entity
        {
        }
        
        // same signature as Update() but encapsulating by Expression enabling getting the MemberExpression to the given field
        // E.g.: o.customer.Entity.lastName
        public static void Update2<TEntity, TValue>(TEntity entity, Expression<Func<TEntity, TValue>> setterExp) where TEntity : Entity
        {
            var entityParameterExpression = ((MemberExpression) setterExp.Body).Expression;
        }

        public static Expression<Action<TEntity>> Set<TEntity, TValue>(
            Expression<Func<TEntity, TValue>> propertyGetExpression,
            Expression<Func<TValue>> valueExpression)
        {
            var entityParameterExpression = (ParameterExpression)
                (((MemberExpression)(propertyGetExpression.Body)).Expression);

            return Expression.Lambda<Action<TEntity>>(
                Expression.Assign(propertyGetExpression.Body, valueExpression.Body),
                entityParameterExpression);
        }

    }

}