// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public readonly struct DatabaseFilter
    {
        internal   readonly     SmallString database;
        internal   readonly     bool        isPrefix;
        internal   readonly     string      dbLabel;

        public     override     string      ToString()  => dbLabel;

        internal DatabaseFilter (string database) {
            dbLabel     = database ?? throw new ArgumentNullException(nameof(database));
            isPrefix    = database.EndsWith("*");
            if (isPrefix) {
                this.database = new SmallString(database.Substring(0, database.Length - 1));
            } else {
                this.database = new SmallString(database);
            }
        }
        
        private bool Authorize(in SmallString databaseName) {
            if (databaseName.IsNull()) throw new ArgumentNullException(nameof(databaseName));
            if (isPrefix) {
                return databaseName.value.StartsWith(database.value);
            }
            return databaseName.IsEqual(database);
        }
        
        internal bool Authorize(SyncContext syncContext) {
            return Authorize(syncContext.databaseName);
        }
        
        internal static bool IsAuthorizedDatabase(IEnumerable<DatabaseFilter> databaseFilters, in SmallString databaseName) {
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