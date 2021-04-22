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

        public void Add(SetInfo info) {
            peers       += info.peers;
            tasks       += info.tasks;
            create      += info.create;
            read        += info.read;
            readRef     += info.readRef;
            query       += info.query;
            patch       += info.patch;
            delete      += info.delete;
        }

        private static void  Add(StringBuilder sb, string label, int count, ref bool first) {
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
                Add(sb, "tasks", tasks, ref first);
                first = true;
                sb.Append(" (");
                Add(sb, "create",   create,    ref first);
                Add(sb, "read",     read,      ref first);
                if (readRef > 0) {
                    sb.Append("(");
                    Add(sb, "ref",      readRef,    ref first);
                    sb.Append(")");
                }
                Add(sb, "query",    query,      ref first);
                Add(sb, "patch",    patch,      ref first);
                Add(sb, "delete",   delete,     ref first);
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}