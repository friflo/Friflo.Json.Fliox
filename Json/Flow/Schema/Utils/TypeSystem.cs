// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITypeSystem
    {
        ITyp   Boolean     { get; }
        ITyp   String      { get; }
        
        ITyp   Unit8       { get; }
        ITyp   Int16       { get; }
        ITyp   Int32       { get; }
        ITyp   Int64       { get; }
        
        ITyp   Float       { get; }
        ITyp   Double      { get; }
        
        ITyp   BigInteger  { get; }
        ITyp   DateTime    { get; }
    }
    
    public class NativeTypeSystem : ITypeSystem
    {
        private readonly NativeType boolean     = new NativeType(typeof(bool),          null);
        private readonly NativeType @string     = new NativeType(typeof(string),        null);
        private readonly NativeType uint8       = new NativeType(typeof(byte),          null);
        private readonly NativeType int16       = new NativeType(typeof(short),         null);
        private readonly NativeType int32       = new NativeType(typeof(int),           null);
        private readonly NativeType int64       = new NativeType(typeof(long),          null);
        private readonly NativeType flt32       = new NativeType(typeof(float),         null);
        private readonly NativeType flt64       = new NativeType(typeof(double),        null);
        private readonly NativeType bigInteger  = new NativeType(typeof(BigInteger),    null);
        private readonly NativeType dateTime    = new NativeType(typeof(DateTime),      null);
        
        
        public ITyp Boolean    => boolean;
        public ITyp String     => @string;
        public ITyp Unit8      => uint8;
        public ITyp Int16      => int16;
        public ITyp Int32      => int32;
        public ITyp Int64      => int64;
        public ITyp Float      => flt32;
        public ITyp Double     => flt64;
        public ITyp BigInteger => bigInteger;
        public ITyp DateTime   => dateTime;
    }
}