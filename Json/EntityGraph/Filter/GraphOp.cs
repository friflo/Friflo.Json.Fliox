// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.EntityGraph.Filter
{

    public abstract class GraphOp { }
    
    // -------------------- unary operators --------------------
    
    // op: Field, String-/Number Literal, Not
    
    public class Field : GraphOp
    {
        public string       field;
    }
    
    public class StringLiteral : GraphOp
    {
        public string       value;
    }
    
    public class NumberLiteral : GraphOp
    {
        public double       value;  // or long
    }
    
    public class BooleanLiteral : GraphOp
    {
        public bool         value;
    }
    
    public class NotOp : GraphOp
    {
        public BoolOp       lambda;
    }

    
    // -------------------- binary operators --------------------

    // op: Equals, NotEquals, Add, Subtract, Multiply, Divide, Remainder, Min, Max, ...
    //     All, Any, Count, Min, Max, ...

    public abstract class BoolOp : GraphOp { }
    
    public class Equals : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }
    
    public class LessThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }
    
    public class GreaterThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }

    public class Any : BoolOp
    {
        public GraphOp      array;      // Field referencing an enumerable
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
    }
    
    public class All : BoolOp
    {
        public GraphOp      array;      // Field referencing an enumerable
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
    }
    

    public class TestOperator
    {
        static void Test () {

            var equals = new Equals {
                left  = new Field           { field = "customer.name"  },
                right = new StringLiteral   { value = "Smith"          }
            };
        }
    }
}