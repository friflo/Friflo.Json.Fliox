// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;

namespace Friflo.Json.Fliox.Hub.Client
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct SetInfo
    {
        public  string  Name  { get; }  // used property to show on top of all members
        
        public  int     peers;
        public  int     tasks;
        //
        public  int     create;
        public  int     upsert;
        public  int     read;
        public  int     query;
        public  int     aggregate;
        public  int     closeCursors;
        public  int     subscribeChanges;
        public  int     patch;
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
            patch           = 0;
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

        private static void  Append(StringBuilder sb, string label, int count, ref bool first) {
            if (count == 0)
                return;
            if (!first) {
                sb.Append(", ");
            }
            sb.Append(label);
            sb.Append(" #");
            sb.Append(count);
            first = false;
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
                Append(sb,      "patch",        patch,          ref first);
                AppendTasks(sb, "delete",       delete,         ref first);
                AppendTasks(sb, "reserveKeys",  reserveKeys,    ref first);
                sb.Append(']');
            }
            return sb.ToString();
        }
        
        public static int Any<TCol>    (ICollection<TCol> col) { return col != null ? col.Count != 0 ? 1 : 0 : 0; }
        public static int Count<TCol>  (ICollection<TCol> col) { return col?.Count ?? 0; }
    }

#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct StoreInfo
    {
        public  int     peers;
        public  int     tasks;
        public  int     commands;
        public  int     messages;

        internal StoreInfo(SyncStore sync, Dictionary<Type, EntitySet> setByType) {
            peers       = 0;
            tasks       = 0;
            commands    = 0;    
            messages    = 0;
            foreach (var pair in setByType)
                Add(pair.Value.SetInfo);
            foreach (var function in sync.functions) {
                switch (function) {
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
        
        public override string ToString() {
            var sb = new StringBuilder();
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
            return sb.ToString();
        }
    } 
}
