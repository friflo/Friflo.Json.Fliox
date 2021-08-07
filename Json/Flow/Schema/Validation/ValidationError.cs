// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Flow.Schema.Validation
{
    public readonly struct ValidationError
    {
        public  readonly    string          msg;
        public  readonly    string          was;
        public  readonly    string          expect;
        public  readonly    ValidationType  type;
        public  readonly    string          path;
        public  readonly    int             pos;

        public  override    string          ToString() => msg == null ? "no error" : AsString(new StringBuilder(), false);

        public ValidationError (string msg, string was, string expect, ValidationType type, string path, int pos) {
            this.msg    = msg;
            this.was    = was;
            this.expect = expect;
            this.type   = type;
            this.path   = path;
            this.pos    = pos;
        }
        
        public ValidationError (string msg, ValidationType type, string path, int pos) {
            this.msg    = msg;
            this.was    = null;
            this.expect = null;
            this.type   = type;
            this.path   = path;
            this.pos    = pos;
        }
        
        public string AsString (StringBuilder sb, bool qualifiedTypeErrors) {
            sb.Clear();
            sb.Append(msg);
            if (was != null) {
                sb.Append(" Was: "); sb.Append(was); sb.Append(", expect: "); sb.Append(expect);
            }
            sb.Append(" ");
            if (type != null) {
                sb.Append("at ");
                var typeName = ValidationType.GetName(type, qualifiedTypeErrors);
                sb.Append(typeName);
                sb.Append(" > ");
            }
            sb.Append(path);
            sb.Append(", pos: ");
            sb.Append(pos);
            return sb.ToString();
        }
    }
}