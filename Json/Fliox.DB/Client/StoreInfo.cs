// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.DB.Client.Internal;

namespace Friflo.Json.Fliox.DB.Client
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct SetInfo
    {
        public readonly string name;
        
        public  int     peers;
        public  int     tasks;
        //
        public  int     create;
        public  int     upsert;
        public  int     reads;
        public  int     readRefs;
        public  int     queries;
        public  int     patch;
        public  int     delete;

        internal SetInfo(string name) {
            this.name = name;
            peers   = 0;
            tasks   = 0;
            //
            create      = 0;
            upsert      = 0;
            reads       = 0;
            readRefs    = 0;
            queries     = 0;
            patch       = 0;
            delete      = 0;
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
            AppendName(sb, name);
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                AppendTasks(sb, "tasks",    tasks,      ref first);
                first = true;
                sb.Append(" >> ");
                Append(sb,  "create",       create,     ref first);
                Append(sb,  "upsert",       upsert,     ref first);
                AppendTasks(sb,  "reads",   reads,      ref first);
                if (readRefs > 0) {
                    sb.Append("(");
                    Append(sb, "refs",      readRefs,    ref first);
                    sb.Append(")");
                }
                AppendTasks(sb, "queries",  queries,    ref first);
                Append(sb,  "patch",        patch,      ref first);
                Append(sb,  "delete",       delete,     ref first);
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

        internal StoreInfo(SyncStore sync, Dictionary<Type, EntitySet> setByType) {
            peers = 0;
            tasks = 0;
            tasks += SetInfo.Count(sync.messageTasks);
            foreach (var pair in setByType)
                Add(pair.Value.SetInfo);
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
            }
            return sb.ToString();
        }
    } 
}
