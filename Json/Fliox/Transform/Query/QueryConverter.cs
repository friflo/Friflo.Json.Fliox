// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Map.Val;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable MergeIntoPattern
namespace Friflo.Json.Fliox.Transform.Query
{
    public static class QueryConverter
    {
        private static readonly QueryPath DefaultQueryPath = new QueryPath();
        
        public static Operation OperationFromExpression(Expression query, QueryPath queryPath) {
            if (queryPath == null) {
                queryPath = DefaultQueryPath;
            }
            var queryCx = new QueryCx(query, queryPath);
            if (query is LambdaExpression lambda) {
                var param   = lambda.Parameters[0].Name;
                var cx      = new LambdaCx (param, "", queryCx);
                var body    = lambda.Body;
                return TraceExpression(body, cx);
            }
            throw new NotSupportedException($"query not supported: {query}");
        }
        
        private static Operation TraceExpression(Expression expression, LambdaCx cx, BinaryExpression binBase = null) {
            switch (expression) {
                case MemberExpression member:
                    return GetMember(member, cx);
                case MethodCallExpression methodCall:
                    return OperationFromMethodCallExpression(methodCall, cx);
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    return TraceExpression(body, cx);
                case UnaryExpression unary:
                    return OperationFromUnaryExpression(unary, cx);
                case BinaryExpression binary:
                    return OperationFromBinaryExpression(binary, cx);
                case ConstantExpression constant:
                    return OperationFromConstant(constant.Value, constant.Type, binBase, cx);
                case ParameterExpression parameter:
                    return new Field(parameter.Name);
                default:
                    throw NotSupported($"Body not supported: {expression}", cx);
            }
        }

        private static bool IsEnumerable(Type type) {
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        
        /// <summary>
        /// A path of chained fields (members) in C# starts at the root.    field_root.field_1. ... .field_leaf
        /// In the LINQ expression tree its starts at the leaf (field_n).   field_leaf -> ... -> field_1 -> field_root
        ///  
        /// The root field (member) decide which <see cref="Operation"/> need to be created.
        /// - If the root is a <see cref="ParameterExpression"/> a <see cref="Field"/> is created.
        ///   (*) In this case the root is a lambda argument.
        /// - If the root is a <see cref="ConstantExpression"/> a <see cref="Literal"/> is created
        ///   from the value return by the leaf field.
        ///   (*) In this case the root references a variable declared outside the lambda expression.
        /// - If the root is <c>null</c> <see cref="Literal"/> is created
        ///   from the value return by the leaf field.
        ///   (*) In this case the root references a static class field / property.
        /// </summary>
        private static Operation GetMember(MemberExpression member, LambdaCx cx) {
            var root = GetRootExpression(member.Expression);
            switch (root) {
                case ParameterExpression parameter: {
                    // case: root is a lambda parameter
                    var linqOperation = GetLinqMemberOperation(member, parameter, cx);
                    if (linqOperation != null) {
                        return linqOperation;
                    }
                    var memberPath  = cx.query.queryPath.GetMemberPath(member, cx);
                    return new Field(parameter.Name + "." + memberPath);
                }
                case null: {
                    // case: root is a reference to a static class field / property 
                    var value = GetMemberValue(member);
                    return OperationFromValue(value, member.Type);
                }
                case ConstantExpression: {
                    // case: root references a variable declared outside the lambda expression.
                    var value = GetMemberValue(member);
                    return OperationFromValue(value, member.Type);
                }
                default:
                    throw NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }
        
        private static Expression GetRootExpression(Expression expression) {
            while (true) {
                switch (expression) {
                    case null:
                        return null;        // member is a reference to a static class field / property 
                    case ParameterExpression parameter:
                        return parameter;   // member represents a lambda parameter
                    case MemberExpression parentMember:
                        expression = parentMember.Expression;
                        break;              // traverse expression tree further to get to the root 
                    case ConstantExpression constant:
                        return constant;    // member is a reference to a variable declared outside the lambda scope
                    default:
                        throw new InvalidOperationException($"unexpected expression: {expression}");
                }
            }
        }
        
        private static object GetMemberValue(MemberExpression member)
        {
            object value;
            switch (member.Expression) {
                case null:                          // case: static class field / property 
                    value = null;
                    break;
                case ConstantExpression constant:   // case: member is reference to variable declared outside lambda scope
                    value = constant.Value;
                    break;
                case MemberExpression parentMember: // case: class instance field / property
                    value = GetMemberValue(parentMember);
                    if (value == null) throw new NullReferenceException($"member: {member.Member.Name}, type: {parentMember.Type.Name}");
                    break;
                default:
                    throw new InvalidOperationException($"unexpected member.Expression: {member.Expression}");
            }
            var memberInfo = member.Member;
            try {
                switch (memberInfo) {
                    case FieldInfo      field:      return field.GetValue(value);
                    case PropertyInfo   property:   return property.GetValue(value);
                }
            } catch (TargetInvocationException e) {
                var type    = memberInfo.DeclaringType?.Name ?? member.Type.Name;
                throw new TargetInvocationException($"property: {memberInfo.Name}, type: {type}", e.InnerException);
            }
            throw new InvalidOperationException($"unexpected member type: {memberInfo}");
        }

        public static string GetMemberName(MemberExpression member, LambdaCx cx) {
            MemberInfo memberInfo = member.Member;
            switch (memberInfo) {
                case FieldInfo:
                case PropertyInfo:
                    return AttributeUtils.GetMemberJsonName(memberInfo);
                default:
                    throw NotSupported($"Member not supported: {member}", cx);
            }
        }
        
        /// <summary>
        /// Return an operation if path ends with <c>.Length</c> or <c>.Count</c>
        /// </summary>
        private static Operation GetLinqMemberOperation(MemberExpression member, ParameterExpression parameter, LambdaCx cx) {
            if (member.Expression is not MemberExpression parentMember) {
                return null;
            }
            var name = GetMemberName(member, cx);
            if (name == "Count" && IsEnumerable(parentMember.Type)) {
                var memberPath  = cx.query.queryPath.GetMemberPath(parentMember, cx);
                var field       = new Field(parameter.Name + "." + memberPath);
                return new Count(field);
            }
            if (name == "Length" && IsEnumerable(parentMember.Type)) {
                var memberPath  = cx.query.queryPath.GetMemberPath(parentMember, cx);
                var field       = new Field(parameter.Name + "." + memberPath);
                return new Length(field);
            }
            return null;
        }

        private static bool IsBclMethod(MethodInfo methodInfo) {
            var declType    = methodInfo.DeclaringType;
            return
                // arithmetic methods in Math like: Abs(), Ceiling(), ...
                declType == typeof (Math)       ||
                // all LINQ methods operating on System.Linq.Enumerable like: Any(), All(), Min(), Max(), ... 
                declType == typeof (Enumerable) || 
                // string methods: Contains(), StartsWith(), EndsWith()
                declType == typeof (string);
        }

        private static Operation OperationFromMethodCallExpression(MethodCallExpression methodCall, LambdaCx cx) {
            if (IsBclMethod(methodCall.Method)) {
                var operation = OperationFromBclMethodCallExpression(methodCall, cx);
                if (operation != null) {
                    return operation;
                }
            }
            // Invoke the method and return its constant result as an operation. 
            var lambda  = Expression.Lambda(methodCall).Compile();
            var method  = methodCall.Method;
            try {
                var value   = lambda.DynamicInvoke();
                var type    = method.ReturnType;
                return OperationFromValue(value, type);
            } catch (TargetInvocationException e) {
                var type    = method.DeclaringType?.Name ?? methodCall.Type.Name;
                throw new TargetInvocationException($"method: {method.Name}, type: {type}", e.InnerException);
            }
        }

        private static Operation OperationFromBclMethodCallExpression(MethodCallExpression methodCall, LambdaCx cx) {
            switch (methodCall.Method.Name) {
                // quantify operations
                case "Any":
                case "All":
                    return OperationFromBinaryQuantifier(methodCall, cx);
                
                // arithmetic operations
                case "Abs":
                case "Ceiling":
                case "Floor":
                case "Exp":
                case "Log":
                case "Sqrt":
                    return OperationFromUnaryArithmetic(methodCall, cx);
                
                // aggregate operations
                case "Min":
                case "Max":
                case "Sum":
                case "Count":
                case "Average":
                    return OperationFromAggregate(methodCall, cx);
                case "Contains":
                case "StartsWith":
                case "EndsWith":
                    return OperationFromBinaryCall(methodCall, cx);
                default:
                    var argument    = methodCall.Arguments[0];
                    if (GetRootExpression(argument) is ParameterExpression) {
                        throw NotSupported($"unsupported method: {methodCall.Method.Name}", cx);
                    }
                    return null;
            }
        }

        private static Operation OperationFromBinaryQuantifier(MethodCallExpression methodCall, LambdaCx cx) {
            var args            = methodCall.Arguments;
            var predicate       = (LambdaExpression)args[1];
            var lambdaParameter = predicate.Parameters[0];
            if (GetRootExpression(lambdaParameter) is not ParameterExpression) {
                throw new NotSupportedException($"Any() and All() must be used on lambda parameter. was: {methodCall}");
            }
            var source          = args[0];
            var sourceOp        = TraceExpression(source, cx);
            if (sourceOp is not Field field) {
                return null;
            }
            var paramName       = lambdaParameter.Name;
            var lambdaCx        = new LambdaCx(paramName, cx.path + field.name, cx.query);
            var predicateOp     = (FilterOperation)TraceExpression(predicate, lambdaCx);
            
            switch (methodCall.Method.Name) {
                case "Any":     return new Any(field, paramName, predicateOp);
                case "All":     return new All(field, paramName, predicateOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromUnaryArithmetic(MethodCallExpression methodCall, LambdaCx cx) {
            var value       = methodCall.Arguments[0];
            var valueOp     = TraceExpression(value, cx);
            switch (methodCall.Method.Name) {
                // --- arithmetic operations
                case "Abs":     return new Abs      (valueOp);
                case "Ceiling": return new Ceiling  (valueOp);
                case "Floor":   return new Floor    (valueOp);
                case "Exp":     return new Exp      (valueOp);
                case "Log":     return new Log      (valueOp);
                case "Sqrt":    return new Sqrt     (valueOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromAggregate(MethodCallExpression methodCall, LambdaCx cx) {
            var     args        = methodCall.Arguments;
            var     arg         = args[0];
            var     sourceOp    = TraceExpression(arg, cx);
            if (sourceOp is not Field field) {
                return null;
            }
            switch (methodCall.Method.Name) {
                case "Count":
                    if (args.Count >= 2) {
                        var predicate           = (LambdaExpression)args[1];
                        var lambdaParameter     = predicate.Parameters[0].Name;
                        var lambdaCxCount       = new LambdaCx(lambdaParameter, cx.path, cx.query);
                        var predicateOp         = (FilterOperation)TraceExpression(predicate, lambdaCxCount);
                        
                        return new CountWhere(field, lambdaParameter, predicateOp);
                    }
                    return new Count(field);
                case "Min":
                case "Max":
                case "Sum":
                case "Average": {
                    var lambdaCx = new LambdaCx(cx.parameter, cx.path + field.name, cx.query);
                    return OperationFromBinaryAggregate(methodCall, lambdaCx);
                }
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromBinaryCall(MethodCallExpression methodCall, LambdaCx cx) {
            var argument    = methodCall.Arguments[0];
            /* if (GetRootExpression(argument) is not ParameterExpression) {
                return null;
            } */
            var leftOp      = TraceExpression(methodCall.Object,       cx);
            var rightOp     = TraceExpression(argument, cx);
            switch (methodCall.Method.Name) {
                // --- binary comparison operations
                case "Contains":            return new Contains     (leftOp, rightOp);
                case "StartsWith":          return new StartsWith   (leftOp, rightOp);
                case "EndsWith":            return new EndsWith     (leftOp, rightOp);

                default:
                    throw NotSupported($"Method not supported. method: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromBinaryAggregate(MethodCallExpression methodCall, LambdaCx cx) {
            var args        = methodCall.Arguments;
            var methodName  = methodCall.Method.Name;
            if (args[1] is LambdaExpression predicate) {
                string sourceField  = cx.path;
                var lambdaParameter = predicate.Parameters[0].Name;
                var lambdaCx        = new LambdaCx(lambdaParameter, cx.path, cx.query);
                var valueOp         = TraceExpression(predicate, lambdaCx);
            
                switch (methodName) {
                    case "Min":     return new Min      (new Field(sourceField), lambdaParameter, valueOp);
                    case "Max":     return new Max      (new Field(sourceField), lambdaParameter, valueOp);
                    case "Sum":     return new Sum      (new Field(sourceField), lambdaParameter, valueOp);
                    case "Average": return new Average  (new Field(sourceField), lambdaParameter, valueOp);
                    default:
                        throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
                }
            }
            throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            /* switch (methodName) {
                case "Min":
                case "Max":
                    break;
                default:
                    
            }
            var leftOp  = TraceExpression(args[0], cx);
            var rightOp = TraceExpression(args[1], cx); */
        }

        private static Operation OperationFromUnaryExpression(UnaryExpression unary, LambdaCx cx) {
            var operand = TraceExpression(unary.Operand, cx);
            switch (unary.NodeType) {
                case ExpressionType.Not:            return new Not((FilterOperation)operand);
                case ExpressionType.Negate:         return new Negate(operand);
                case ExpressionType.Convert:
                    return OperationFromConvert(unary, cx);
                default:
                    throw NotSupported($"UnaryExpression not supported: {unary}", cx);
            }
        }

        private static Operation OperationFromConvert(UnaryExpression unary, LambdaCx cx) {
            var type = unary.Operand.NodeType;
            switch (type) {
                /*
                case ExpressionType.Constant:
                case ExpressionType.Call:
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Negate:
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                    return TraceExpression(unary.Operand, cx); */
                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)unary.Operand;
                    return GetMember(member, cx);
                default:
                    return TraceExpression(unary.Operand, cx);
                    // throw NotSupported($"Convert Operand not supported. operand: {type}", cx);
            }
        }

        private static Operation OperationFromBinaryExpression(BinaryExpression binary, LambdaCx cx) {
            var leftOp  = TraceExpression(binary.Left,  cx, binary);
            var rightOp = TraceExpression(binary.Right, cx, binary);
            switch (binary.NodeType) {
                // --- binary comparison operations
                case ExpressionType.Equal:              return new Equal            (leftOp, rightOp);
                case ExpressionType.NotEqual:           return new NotEqual         (leftOp, rightOp);
                case ExpressionType.LessThan:           return new Less             (leftOp, rightOp);
                case ExpressionType.LessThanOrEqual:    return new LessOrEqual      (leftOp, rightOp);
                case ExpressionType.GreaterThan:        return new Greater          (leftOp, rightOp);
                case ExpressionType.GreaterThanOrEqual: return new GreaterOrEqual   (leftOp, rightOp);
                
                // --- group operations:
                case ExpressionType.OrElse:             return new Or (new List<FilterOperation> {(FilterOperation)leftOp, (FilterOperation)rightOp});
                case ExpressionType.AndAlso:            return new And(new List<FilterOperation> {(FilterOperation)leftOp, (FilterOperation)rightOp});
                
                // --- binary arithmetic operations
                case ExpressionType.Add:                return new Add                  (leftOp, rightOp);
                case ExpressionType.Subtract:           return new Subtract             (leftOp, rightOp);
                case ExpressionType.Multiply:           return new Multiply             (leftOp, rightOp);
                case ExpressionType.Divide:             return new Divide               (leftOp, rightOp);
                case ExpressionType.Modulo:             return new Modulo               (leftOp, rightOp);

                default:
                    throw NotSupported($"Method not supported. method: {binary}", cx);
            }
        }
        
        /// <summary>
        /// Required to determine the type of enums as LINQ convert enums to their underlying enum type like int32, ...  
        /// </summary>
        private static bool GetBinaryConvertType(BinaryExpression binary, out Type convertType) {
            var left = binary.Left;
            if (left.NodeType == ExpressionType.Convert && left is UnaryExpression unaryLeft) {
                convertType = unaryLeft.Operand.Type;
                return true;
            }
            var right = binary.Right;
            if (right.NodeType == ExpressionType.Convert && right is UnaryExpression unaryRight) {
                convertType = unaryRight.Operand.Type;
                return true;
            }
            convertType = null;
            return false;
        }
        
        private static Operation OperationFromConstant(object value, Type type, BinaryExpression binBase, LambdaCx cx) {
            if (binBase != null) {
                if (GetBinaryConvertType(binBase, out var convertType)) {
                    type = convertType;
                }
            }
            /* if (type.IsDefined (typeof (CompilerGeneratedAttribute), false)) {
                var fields  = type.GetFields();
                var field   = fields[0];
                value       = field.GetValue(value);
                type        = field.FieldType;
            }*/
            var operation   =  OperationFromValue(value, type);
            if (operation == null) throw NotSupported($"Constant not supported: type: {type.FullName}, value: {value}", cx);
            return operation;
        }

        private static Operation OperationFromValue(object value, Type type)
        {
            // --- null
            if (value == null) {
                return new NullLiteral();
            }
            if (type == typeof(string))     return new StringLiteral((string)   value);
            
            // --- floating point
            if (type == typeof(double)) {
                var doubleValue = (double)   value;
                if (doubleValue == Math.E)  return new EulerLiteral();
                if (doubleValue == Math.PI) return new PiLiteral();
                                            return new DoubleLiteral(doubleValue);
            }
            if (type == typeof(float))      return new DoubleLiteral((float)    value);
            
            // --- integral
            if (type == typeof(long))       return new LongLiteral((long)       value);
            if (type == typeof(int))        return new LongLiteral((int)        value);
            if (type == typeof(short))      return new LongLiteral((short)      value);
            if (type == typeof(byte))       return new LongLiteral((byte)       value);
            
            // --- bool
            if (type == typeof(bool)) {
                if ((bool) value)
                    return Operation.FilterTrue;
                return Operation.FilterFalse;
            }
            if (type == typeof(char)) {
                var c = (char)value;
                return new StringLiteral(c.ToString());
            }
            if (type == typeof(DateTime)) {
                var str = DateTimeMapper.ToRFC_3339((DateTime)value);
                return new StringLiteral(str);
            }
            if (type.IsEnum) {
                var valueName = Enum.GetName(type, value);
                return new StringLiteral(valueName);
            }
            return null;
        }

        public static Exception NotSupported(string message, LambdaCx cx) {
            return new NotSupportedException($"{message}, expression: {cx.query.exp}");
        }
    }
}