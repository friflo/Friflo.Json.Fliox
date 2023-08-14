// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    /// <summary>
    /// <see cref="DateTime"/> values are persisted in all databases as UTC.<br/>
    /// - in Key/Value or document databases as ISO 8601 UTC using suffix Z (Zulu) see <see cref="Bytes.DateTimeFormat"/><br/>
    /// - in SQL relational tables as Timestamp (without time zone). 
    /// </summary>
    /// <remarks>
    /// grep:
    ///     DateTime.TryParse
    ///     DateTime.SpecifyKind
    ///     DateTime2Lng
    ///     Lng2DateTime
    ///     ToUniversalTime
    /// </remarks>
    public static class TestDateTimeMapper
    {
        private const string StringUtc  =   "2021-01-14T11:11:00.555Z";
        private const string UtcJSON    = "\"2021-01-14T11:11:00.555Z\"";
        
        private static readonly DateTime DateTimeUtc;
        
        static TestDateTimeMapper() {
            DateTimeUtc = DateTime.Parse(StringUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }
        
        
        [Test]
        public static void TestDateTimeMapper_ReadWrite_UTC() {
            var utc = DateTime.Parse("2021-01-14T11:11:00.555Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            AreEqual(DateTimeKind.Utc, utc.Kind);
            Assert_ReadWrite(utc);
        }
        
        [Test]
        public static void TestDateTimeMapper_ReadWrite_UTC_Local() {
            var local = DateTime.SpecifyKind(DateTimeUtc, DateTimeKind.Local);
            AreEqual(DateTimeKind.Local, local.Kind);
            Assert_ReadWrite(DateTimeUtc);
        }
        
        private static void Assert_ReadWrite(DateTime expect) {
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);
            
            DateTime value  = mapper.Read<DateTime>(UtcJSON);
            
            var expectLocal = DateTime.SpecifyKind(expect, DateTimeKind.Local);
            AreEqual(expectLocal,       value);
            AreEqual(DateTimeKind.Utc,  value.Kind);
                    
            var result = mapper.Write(expect);
            AreEqual(UtcJSON, result);
                    
            // Nullable
            DateTime? nullExpect    = expect;
            DateTime? nullValue     = mapper.Read<DateTime?>(UtcJSON);
            AreEqual(expect,            nullValue!.Value);
            AreEqual(DateTimeKind.Utc,  nullValue.Value.Kind);
                    
            result = mapper.Write(nullExpect);
            AreEqual(UtcJSON, result);
        }
        
        // [Test]
        public static void TestDateTimeMapper_Test() {
            var unspecified = DateTime.Parse("2021-01-14 11:00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            AreEqual(DateTimeKind.Unspecified, unspecified.Kind);
            
            var local = unspecified.ToLocalTime();
            AreEqual(DateTimeKind.Utc, local.Kind);
            
            var utc = unspecified.ToUniversalTime();
            
            Assert_ReadWrite(utc);
        }
    }
}