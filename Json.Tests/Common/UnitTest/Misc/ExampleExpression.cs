using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class ExampleExpression
    {
        [Test]
        public void Run() {
            var items = new List<Test>() {
                new Test() { Parameter = "Alpha" },
                new Test(),
                new Test() { Parameter = "Test" },
                new Test() { Parameter = "test" },
                new Test() { Parameter = "TEST" },
                new Test() { Parameter = "Contains test" }
            };
            var expr = ContainsValue<Test>("Parameter",  "test");
            // you can see the body here
            Console.WriteLine( expr.Body );
            // and the result
            var results = items.Where( expr.Compile() ).Select(t => t.Parameter).ToList();
            Console.WriteLine( "Results: {0}", string.Join( ",", results ));
            Console.WriteLine( "Total results: {0}", results.Count );
        }

        public class Test {
            public string Parameter { get;set; }
        }
        
        public static Expression<Func<T, bool>> ContainsValue<T>(string fieldName, string val) {
            var type = typeof(T);
            var member = Expression.Parameter(type, "param");
            var memberExpression = Expression.PropertyOrField( member, fieldName);
            var targetMethod = memberExpression.Type.GetMethod( "IndexOf", new Type[] { typeof(string), typeof(StringComparison) } );
            var methodCallExpression = Expression.Call( memberExpression, targetMethod, Expression.Constant(val), Expression.Constant( StringComparison.CurrentCultureIgnoreCase ) );

            return Expression.Lambda<Func<T, bool>>( 
                Expression.AndAlso(
                    Expression.NotEqual(memberExpression, Expression.Constant(null)), 
                    Expression.GreaterThanOrEqual( methodCallExpression, Expression.Constant(0) )
                ), 
                member
            );
        }
        

    }
}