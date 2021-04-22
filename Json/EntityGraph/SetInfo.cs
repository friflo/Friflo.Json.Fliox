// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.EntityGraph
{
    public class SetInfo
    {
        public  int     peers;
        public  int     tasks;
        //
        public  int     create;
        public  int     read;
        public  int     readRef;
        public  int     query;
        public  int     patch;
        public  int     delete;

        internal static void  Append(StringBuilder sb, string label, int count, ref bool first) {
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
            sb.Append("peers");
            sb.Append(": ");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                Append(sb, "tasks", tasks, ref first);
                first = true;
                sb.Append(" -> ");
                Append(sb, "create",   create,    ref first);
                Append(sb, "read",     read,      ref first);
                if (readRef > 0) {
                    sb.Append("(");
                    Append(sb, "ref",      readRef,    ref first);
                    sb.Append(")");
                }
                Append(sb, "query",    query,      ref first);
                Append(sb, "patch",    patch,      ref first);
                Append(sb, "delete",   delete,     ref first);
            }
            return sb.ToString();
        }
    }

    public class StoreInfo
    {
        public  int     peers;
        public  int     tasks;
        
        public void Add(SetInfo info) {
            peers       += info.peers;
            tasks       += info.tasks;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("peers");
            sb.Append(": ");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                SetInfo.Append(sb, "tasks", tasks, ref first);
            }
            return sb.ToString();
        }
    } 
}