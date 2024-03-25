// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    /// <see cref="DateTime"/> values are persisted as UTC in all databases providers.<br/>
    /// In case a <see cref="DateTime"/> value is not <see cref="DateTimeKind.Utc"/> the value returned by
    /// <see cref="DateTime.ToUniversalTime"/> is serialized.<br/>
    /// <br/>
    /// Type used to persist a DateTime value:<br/>  
    /// - in Key/Value or document (JSON) databases as ISO 8601 UTC using suffix Z (Zulu) see <see cref="Bytes.DateTimeFormat"/><br/>
    /// - in SQL relational tables as Timestamp (without time zone).<br/>
    /// <br/>
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
            var utc = DateTimeUtc;
            AreEqual(DateTimeKind.Utc, utc.Kind);
            Assert_ReadWrite(utc);
        }

        /// <summary>
        /// <see cref="DateTimeKind.Local"/> DateTime values are serialized as <see cref="DateTimeKind.Utc"/> time.<br/>
        /// Their result of <see cref="DateTime.ToUniversalTime"/> is serialized.
        /// </summary>
        [Test]
        public static void TestDateTimeMapper_ReadWrite_Local() {
            var local = DateTimeUtc.ToLocalTime();
            AreEqual(DateTimeKind.Local, local.Kind);
            Assert_ReadWrite(local);
        }
        
        /// <summary>
        /// <see cref="DateTimeKind.Unspecified"/> DateTime values are assumed to be <see cref="DateTimeKind.Local"/> time.<br/>
        /// See: <see cref="DateTime.ToUniversalTime"/>
        /// </summary>
        [Test]
        public static void TestDateTimeMapper_ReadWrite_Unspecified() {
            var local = DateTimeUtc.ToLocalTime();
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified); 
            AreEqual(DateTimeKind.Unspecified, unspecified.Kind);
            Assert_ReadWrite(unspecified);
        }
        
        private static void Assert_ReadWrite(DateTime dateTime) {
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);
            
            DateTime value  = mapper.Read<DateTime>(UtcJSON);
            AreEqual(DateTimeKind.Utc,  value.Kind);            // deserialized DateTime is always UTC
            
            var dateTimeUtc = dateTime.ToUniversalTime();
            AreEqual(dateTimeUtc,       value);
                    
            var result = mapper.Write(dateTime);                // local times are serialized as UTC
            AreEqual(UtcJSON, result);
                    
            // Nullable
            DateTime? nullExpect    = dateTime;
            DateTime? nullValue     = mapper.Read<DateTime?>(UtcJSON);
            AreEqual(dateTimeUtc,       nullValue!.Value);
            AreEqual(DateTimeKind.Utc,  nullValue.Value.Kind);  // deserialized DateTime is always UTC
                    
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