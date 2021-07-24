// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITypeSystem
    {
        ICollection<ITyp> Types { get;}
        
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
        
        ITyp   JsonValue   { get; }
        
        ICollection<ITyp> GetTypes(ICollection<Type> separateTypes);
    }
}