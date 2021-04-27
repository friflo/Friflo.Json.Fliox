// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Graph
{
    public struct SetInfo
    {
        public readonly string name;
        
        public  int     peers;
        public  int     tasks;
        //
        public  int     create;
        public  int     read;
        public  int     readRefs;
        public  int     queries;
        public  int     patch;
        public  int     delete;

        public SetInfo(string name) {
            this.name = name;
            peers   = 0;
            tasks   = 0;
            //
            create      = 0;
            read        = 0;
            readRefs    = 0;
            queries     = 0;
            patch       = 0;
            delete      = 0;
        }
        
        internal static void  AppendName(StringBuilder sb, string name) {
            sb.Append(name);
            sb.Append(":");
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
                sb.Append(" -> ");
                Append(sb,  "create",       create,     ref first);
                Append(sb,  "read",         read,       ref first);
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
    }

    public struct StoreInfo
    {
        public  int     peers;
        public  int     tasks;

        internal StoreInfo(Dictionary<Type, EntitySet> setByType) {
            peers = 0;
            tasks = 0;
            foreach (var pair in setByType)
                Add(pair.Value.SetInfo);
        }
        
        private void Add(in SetInfo info) {
            peers += info.peers;
            tasks += info.tasks;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            SetInfo.AppendName(sb, "all");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                SetInfo.AppendTasks(sb, "tasks", tasks, ref first);
            }
            return sb.ToString();
        }
    } 
}
