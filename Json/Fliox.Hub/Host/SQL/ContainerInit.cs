// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public interface ISQLDatabase
    {
        public bool AutoCreateDatabase      { get; init; }
        public bool AutoCreateTables        { get; init; }
        public bool AutoAddVirtualColumns   { get; init; }
    }
    
    public interface ISQLTable
    {
        Task<TaskExecuteError>  InitTable           (SyncConnection connection);
        Task                    AddVirtualColumns   (SyncConnection connection);
    }
    
    public class ContainerInit
    {
        public              bool    tableCreated;
        private  readonly   bool    autoCreateTable;
        
        public              bool    virtualColumnsAdded;
        private  readonly   bool    autoAddVirtualColumns;
        
        public              bool    CreateTable         => !tableCreated        && autoCreateTable;
        public              bool    AddVirtualColumns   => !virtualColumnsAdded && autoAddVirtualColumns;
        
        public ContainerInit(ISQLDatabase database) {
            autoCreateTable         = database.AutoCreateTables;
            autoAddVirtualColumns   = database.AutoAddVirtualColumns;
        }
    }
}