// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public abstract class BinaryQuantifyOp : FilterOperation
    {
        [Required]  public  Field           field;
        [Required]  public  string          arg;
        [Required]  public  FilterOperation predicate;  // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp() { }
        protected BinaryQuantifyOp(Field field, string arg, FilterOperation predicate) {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            cx.initArgs.Add(arg);
            field.Init(cx);
            predicate.Init(cx);
        }
    }
    
    public sealed class Any : BinaryQuantifyOp
    {
        public   override string    OperationName           => "Any";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Any", field, arg, predicate, cx);

        public Any() { }
        public Any(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }
        
        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                while (item.HasNext()) {
                    var val     = predicate.Eval(cx);
                    var result  = val.EqualsTo(True, this);
                    if (result.IsError)
                        return result;
                    if (result.IsTrue)
                        return True;
                    item.MoveNext();
                }
                return False;
            }
        }
    }
    
    public sealed class All : BinaryQuantifyOp
    {
        public   override string    OperationName           => "All";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("All", field, arg, predicate, cx);

        public All() { }
        public All(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }

        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                while (item.HasNext()) {
                    var value   = predicate.Eval(cx);
                    var result  = value.EqualsTo(True, this);
                    if (result.IsError)
                        return result;
                    if (!result.IsTrue)
                        return False;
                    item.MoveNext();
                }
                return True;
            }
        }
    }
    
    public sealed class CountWhere : Operation // Note: must not extend: FilterOperation
    {
        [Required]  public          Field           field;
        [Required]  public          string          arg;
        [Required]  public          FilterOperation predicate;  // e.g.   i => i.amount < 1
        
        public CountWhere() { }
        public CountWhere(Field field, string arg, FilterOperation predicate)  {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            cx.initArgs.Add(arg);
            field.Init(cx);
            predicate.Init(cx);
        }

        public   override string    OperationName           => "Count";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Count", field, arg, predicate, cx);

        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                int count = 0;
                while (item.HasNext()) {
                    var value   = predicate.Eval(cx);
                    var result  = value.EqualsTo(True, this);
                    if (result.IsError)
                        return result;
                    if (result.IsTrue) {
                        count++;                        
                    }
                    item.MoveNext();
                }
                return new Scalar(count);
            }
        }
    }
}