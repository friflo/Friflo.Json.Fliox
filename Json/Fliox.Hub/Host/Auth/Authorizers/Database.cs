// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public abstract class AuthorizerDatabase : Authorizer
    {
        private    readonly     string  database;
        private    readonly     bool    isPrefix;
        protected  readonly     string  dbLabel;
        
        protected AuthorizerDatabase () {
            isPrefix    = true;
            database    = "";
            dbLabel     = "*";
        }
        protected AuthorizerDatabase (string database) {
            if (database == null) {
                dbLabel = EntityDatabase.MainDB;
                return;
            }
            dbLabel     = database;
            isPrefix    = database.EndsWith("*");
            if (isPrefix) {
                this.database = database.Substring(0, database.Length - 1);
            } else {
                this.database = database;
            }
        }
        
        protected bool AuthorizeDatabase(ExecuteContext executeContext) {
            var db = executeContext.DatabaseName;
            if (isPrefix) {
                if (db != null) 
                    return db.StartsWith(database);
                return database.Length == 0;
            }
            return db == database;
        }
    }
}