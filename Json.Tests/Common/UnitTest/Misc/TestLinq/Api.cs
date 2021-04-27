using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.TestLinq
{

    public enum Order
    {
        None,
        Asc,
        Desc,
    }

    public static class Graph
    {
        public static TSource TestQuery<TSource, TOrderBy>(
            int                     limit   = 0,
            Func<TSource, TOrderBy> orderBy = null,
            Order                   order   = Order.None,
            Func<TSource, bool>     where   = null,
            Func<TSource>           select  = null
        ) where TSource : class
        {
            return null;
        }
        
        public static void Selector<TEntity, TValue>(TEntity entity, Func<TValue> selector) where TEntity : Entity
        {
        }
        
        public static TValue Sel<TElement, TValue>(this IEnumerable<TElement> e, Func<TElement, TValue> selector) {
            return selector(e.First());
        }

        public static TValue Sel2<TElement, TValue>(this IEnumerable<TElement> e, Expression<Func<TElement, TValue>> selector) {
            return selector.Compile()(e.First());
        }
        
        public static TValue Sub<TClass, TValue>(this TClass e, Func<TClass, TValue> selector) where TClass : class
        {
            return selector(e);
        }

        
        public static void SelectorExpr<TEntity, TValue>(TEntity entity, Expression<Func<TValue>> selector) where TEntity : Entity
        {
        }
        
        public static void Update<TEntity, TValue>(TEntity entity, Func<TEntity, TValue> field) where TEntity : Entity
        {
        }
        
        // same signature as Update() but encapsulating by Expression enabling getting the MemberExpression to the given field
        // E.g.: o.customer.Entity.lastName
        public static void Update2<TEntity, TValue>(TEntity entity, Expression<Func<TEntity, TValue>> setterExp) where TEntity : Entity
        {
            // var entityParameterExpression = ((MemberExpression) setterExp.Body).Expression;
        }
        
        public static void Update3<TEntity>(TEntity entity, Expression<Func<TEntity>> setterExp) where TEntity : Entity
        {
            // var entityParameterExpression = ((MemberExpression) setterExp.Body).Expression;
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