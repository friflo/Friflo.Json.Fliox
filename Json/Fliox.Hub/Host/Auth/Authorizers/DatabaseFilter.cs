// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public readonly struct DatabaseFilter
    {
        internal   readonly     string  database;
        internal   readonly     bool    isPrefix;
        internal   readonly     string  dbLabel;

        public     override     string  ToString()  => dbLabel;

        internal DatabaseFilter (string database) {
            dbLabel     = database ?? throw new ArgumentNullException(nameof(database));
            isPrefix    = database.EndsWith("*");
            if (isPrefix) {
                this.database = database.Substring(0, database.Length - 1);
            } else {
                this.database = database;
            }
        }
        
        private bool Authorize(string databaseName) {
            if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));
            if (isPrefix) {
                return databaseName.StartsWith(database);
            }
            return databaseName == database;
        }
        
        internal bool Authorize(SyncContext syncContext) {
            var databaseName = syncContext.DatabaseName;
            return Authorize(databaseName);
        }
        
        internal static bool IsAuthorizedDatabase(IEnumerable<DatabaseFilter> databaseFilters, string databaseName) {
            foreach (var authorizeDatabase in databaseFilters) {
                if (authorizeDatabase.Authorize(databaseName))
                    return true;
            }
            return false;
        }
    }
    
    internal sealed class DatabaseFilterComparer : IEqualityComparer<DatabaseFilter>
    {
        internal static readonly DatabaseFilterComparer Instance  = new DatabaseFilterComparer();
        
        public bool Equals(DatabaseFilter left, DatabaseFilter right)
        {
            return left.dbLabel == right.dbLabel;
        }

        public int GetHashCode(DatabaseFilter obj)
        {
            return obj.dbLabel.GetHashCode();
        }
    }
}