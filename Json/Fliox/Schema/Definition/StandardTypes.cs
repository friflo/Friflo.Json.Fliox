// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// Contain all standard types used by a <see cref="TypeSchema"/>.
    /// Unused standard types are null.
    /// </summary>
    public abstract class StandardTypes
    {
        public abstract     TypeDef     Boolean     { get; }
        public abstract     TypeDef     String      { get; }
        
        public abstract     TypeDef     Uint8       { get; }
        public abstract     TypeDef     Int16       { get; }
        public abstract     TypeDef     Int32       { get; }
        public abstract     TypeDef     Int64       { get; }
        
        public abstract     TypeDef     Float       { get; }
        public abstract     TypeDef     Double      { get; }
        
        public abstract     TypeDef     BigInteger  { get; }
        public abstract     TypeDef     DateTime    { get; }
        public abstract     TypeDef     Guid        { get; }
        
        public abstract     TypeDef     JsonValue   { get; }
        public abstract     TypeDef     JsonKey     { get; }
        public abstract     TypeDef     ShortString { get; }
        public abstract     TypeDef     JsonEntity  { get; }
    }
    
    public enum StandardTypeId
    {
        None        = 0,
        //
        Boolean     = 1,
        String      = 2,    // used also for ShortString
        // --- integer
        Uint8       = 3,
        Int16       = 4,
        Int32       = 5,
        Int64       = 6,
        // --- floating point
        Float       = 7,
        Double      = 8,
        // --- specialized
        BigInteger  = 9,
        DateTime    = 10,
        Guid        = 11,
        JsonValue   = 12,
        JsonKey     = 13,
        JsonEntity  = 15,
        Enum        = 16,
    }
}