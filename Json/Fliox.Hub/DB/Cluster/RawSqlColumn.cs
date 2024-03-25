// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /** The column type used by an SQL database. */
    public enum RawColumnType
    {
        Unknown     =  0,
        //
        /** Not supported by all SQL database. SQLite, SQL Server, MySQL, MariaDB: tinyint */
        Bool        =  1,
        //
        /** Not supported by all SQL database. SQLite: integer, PostgreSQL: smallint */
        Uint8       =  2,
        /** Not supported by all SQL database. SQLite: integer */
        Int16       =  3,
        /** Not supported by all SQL database. SQLite: integer */
        Int32       =  4,
        Int64       =  5,
        //
        String      =  6,
        /** Not supported by all SQL database. SQLite: text */
        DateTime    =  7,
        /** Not supported by all SQL database. SQLite: text, MySQL: varchar(36) */
        Guid        =  8,
        //
        /** Not supported by all SQL database. SQLite: real */
        Float       =  9,
        Double      = 10,
        //
        /** Not supported by all SQL database. SQLite: text, SQL Server: nvarchar(max), MariaDB: longtext */
        JSON        = 11,
    }
    
    public struct RawSqlColumn
    {
        public string           name;
        public RawColumnType    type;
        
        public RawSqlColumn(string name, RawColumnType type) {
            this.name   = name;
            this.type   = type;
        }
    }
}