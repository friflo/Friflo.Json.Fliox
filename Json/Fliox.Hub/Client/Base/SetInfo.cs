// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Contain the number of tracked entities of an <see cref="EntitySet{TKey,T}"/> and the number of tasks grouped by type.
    /// </summary>
    public struct SetInfo
    {
        /// <summary>container name / <see cref="EntitySet{TKey,T}"/> name</summary>
        public  string  Name  { get; }  // used property to show on top of all members
        
        /// <summary>number of tracked entities in an <see cref="EntitySet{TKey,T}"/></summary>
        public  int     peers;
        /// <summary>current number of tasks <see cref="EntitySet{TKey,T}"/> scheduled by an <see cref="EntitySet{TKey,T}"/></summary>
        public  int     tasks;
        //
        /// <summary>number of create tasks</summary>
        public  int     create;
        /// <summary>number of upsert tasks</summary>
        public  int     upsert;
        /// <summary>number of read tasks</summary>
        public  int     read;
        /// <summary>number of query tasks</summary>
        public  int     query;
        /// <summary>number of aggregate tasks</summary>
        public  int     aggregate;
        /// <summary>number of close query cursor tasks</summary>
        public  int     closeCursors;
        /// <summary>number of container subscription tasks</summary>
        public  int     subscribeChanges;
        /// <summary>number of patch tasks</summary>
        public  int     merge;
        /// <summary>number of delete tasks</summary>
        public  int     delete;
        public  int     reserveKeys;

        internal SetInfo(string name) {
            Name            = name;
            peers           = 0;
            tasks           = 0;
            //
            create          = 0;
            upsert          = 0;
            read            = 0;
            query           = 0;
            aggregate       = 0;
            closeCursors    = 0;
            subscribeChanges= 0;
            merge           = 0;
            delete          = 0;
            reserveKeys     = 0;
        }
        
        internal static void  AppendName(StringBuilder sb, string name) {
            sb.Append(name);
            sb.Append(": ");
            int len = name.Length + 1;
            for (int n = len; n < 10; n++)
                sb.Append(' ');
        }

        internal static void  AppendTasks(StringBuilder sb, string label, int count, ref bool first) {
            if (count == 0)
                return;
            if (!first) {
                sb.Append(", ");
            }
            sb.Append(label);
            sb.Append(": ");
            sb.Append(count);
            first = false;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AppendName(sb, Name);
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                AppendTasks(sb, "tasks",        tasks,          ref first);
                first = true;
                sb.Append(" [");
                AppendTasks(sb, "create",       create,         ref first);
                AppendTasks(sb, "upsert",       upsert,         ref first);
                AppendTasks(sb, "read",         read,           ref first);
                AppendTasks(sb, "query",        query,          ref first);
                AppendTasks(sb, "aggregate",    aggregate,      ref first);
                AppendTasks(sb, "closeCursors", closeCursors,   ref first);
                AppendTasks(sb, "merge",        merge,          ref first);
                AppendTasks(sb, "delete",       delete,         ref first);
                AppendTasks(sb, "reserveKeys",  reserveKeys,    ref first);
                sb.Append(']');
            }
            return sb.ToString();
        }
        
        public static int Any<TCol>    (ICollection<TCol> col) { return col != null ? col.Count != 0 ? 1 : 0 : 0; }
        public static int Count<TCol>  (ICollection<TCol> col) { return col?.Count ?? 0; }
    }

    public struct ClientInfo
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public          string  Database { get; }
        public          int     peers;
        public          int     tasks;
        public          int     commands;
        public          int     messages;
        
        public override string  ToString() => FormatToString();

        internal ClientInfo(FlioxClient client) {
            Database    = client.DatabaseName;
            peers       = 0;
            tasks       = 0;
            commands    = 0;    
            messages    = 0;
            foreach (var set in client.entitySets) {
                if (set == null) continue;
                Add(set.SetInfo);
            }
            foreach (var task in client._intern.syncStore.tasks.GetReadOnlySpan()) {
                switch (task) {
                    case CommandTask _: commands++; break;
                    case MessageTask _: messages++; break;
                }
            }
            tasks += commands + messages;
        }
        
        private void Add(in SetInfo info) {
            peers += info.peers;
            tasks += info.tasks;
        }
        

        private string FormatToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }
        
        internal void AppendTo(StringBuilder sb) {
            SetInfo.AppendName(sb, "entities");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                SetInfo.AppendTasks(sb, "tasks", tasks, ref first);
                sb.Append(" [");
                first = true;
                SetInfo.AppendTasks(sb, "message",  messages,   ref first);
                SetInfo.AppendTasks(sb, "command",  commands,   ref first);
                var containerTasks = tasks - messages - commands;
                SetInfo.AppendTasks(sb, "container",  containerTasks, ref first);
                sb.Append(']');
            }
        }
    } 
}
