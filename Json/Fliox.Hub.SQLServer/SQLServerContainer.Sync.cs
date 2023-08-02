// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Data;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;


namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed partial class SQLServerContainer
    {

        SqlCommand      readCommand;
        SqlParameter    sqlParam;
        
        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            using var pooled = syncContext.SQL2Object.Get();
            var sql2Object = pooled.instance;
            try {
                if (readCommand == null) {
                    var sql = sql2Object.sb;
                    sql.Clear();
                    sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
                    sql.Append($" FROM {name} WHERE {tableInfo.keyColumn.name} in (@ids);\n");
                    readCommand = new SqlCommand(sql.ToString(), connection.instance);
                    sqlParam = readCommand.Parameters.Add("@ids", SqlDbType.NVarChar, 100);
                    readCommand.Prepare();
                }
                sql2Object.sb.Clear();
                sqlParam.Value = SQLUtils.AppendKeysSQL2(sql2Object.sb, command.ids, SQLEscape.PrefixN).ToString();
                using var reader = connection.ExecuteReaderSync(readCommand);

                return SQLTable.ReadObjects(reader, command, sql2Object);
            }
            catch (SqlException e) {
                var msg = GetErrMsg(e);
                return new ReadEntitiesResult { Error = new TaskExecuteError(msg) };
            }
        }
    }
}

#endif