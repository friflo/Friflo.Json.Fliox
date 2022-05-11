// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public readonly struct AuthorizeDatabase
    {
        internal   readonly     string  database;
        internal   readonly     bool    isPrefix;
        internal   readonly     string  dbLabel;

        public     override     string  ToString()  => dbLabel;

        /* protected DatabaseAuthorizer () {
            isPrefix    = true;
            database    = "";
            dbLabel     = "*";
        } */
        
        internal AuthorizeDatabase (string database) {
            dbLabel     = database ?? throw new ArgumentNullException(nameof(database));
            isPrefix    = database.EndsWith("*");
            if (isPrefix) {
                this.database = database.Substring(0, database.Length - 1);
            } else {
                this.database = database;
            }
        }
        
        internal bool Authorize(string db) {
            if (isPrefix) {
                if (db != null) 
                    return db.StartsWith(database);
                return database.Length == 0;
            }
            return db == database;
        }
        
        internal bool Authorize(ExecuteContext executeContext) {
            var databaseName = executeContext.DatabaseName ?? executeContext.hub.database.name;;
            return Authorize(databaseName);
        }
    }
    
    internal class AuthorizeDatabaseComparer : IEqualityComparer<AuthorizeDatabase>
    {
        internal static readonly AuthorizeDatabaseComparer Instance  = new AuthorizeDatabaseComparer();
        
        public bool Equals(AuthorizeDatabase left, AuthorizeDatabase right)
        {
            return left.dbLabel == right.dbLabel;
        }

        public int GetHashCode(AuthorizeDatabase other)
        {
            return other.GetHashCode();
        }
    }
}