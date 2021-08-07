// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Flow.Schema.Validation
{
    public readonly struct ValidationError
    {
        public  readonly    string          msg;
        public  readonly    string          value;
        public  readonly    bool            isString;
        public  readonly    string          expect;
        public  readonly    string          expectNamespace;
        public  readonly    ValidationType  type;
        public  readonly    string          path;
        public  readonly    int             pos;

        public  override    string          ToString() => msg == null ? "no error" : AsString(new StringBuilder(), false);

        public ValidationError (string msg, string value, bool isString, string expect, string expectNamespace, ValidationType type, string path, int pos) {
            this.msg            = msg;
            this.value          = value;
            this.isString       = isString;
            this.expect         = expect;
            this.expectNamespace= expectNamespace;
            this.type           = type;
            this.path           = path;
            this.pos            = pos;
        }
        
        public ValidationError (string msg, string value, bool isString, ValidationType type, string path, int pos) {
            this.msg            = msg;
            this.value          = value;
            this.isString       = isString;
            this.expect         = null;
            this.expectNamespace= null;
            this.type           = type;
            this.path           = path;
            this.pos            = pos;
        }
        
        public string AsString (StringBuilder sb, bool qualifiedTypeErrors) {
            sb.Clear();
            sb.Append(msg);
            if (value != null) {
                if (expect != null) {
                    sb.Append(" Was: ");
                    if (isString) {
                        sb.Append('\'');
                        sb.Append(value);
                        sb.Append('\'');
                    } else {
                        sb.Append(value);
                    }
                    sb.Append(", expect: ");
                    if (qualifiedTypeErrors && expectNamespace != null) {
                        sb.Append(expectNamespace);
                        sb.Append('.');
                    }
                    sb.Append(expect);
                } else {
                    sb.Append(' ');
                    if (isString) {
                        sb.Append('\'');
                        sb.Append(value);
                        sb.Append('\'');
                    } else {
                        sb.Append(value);
                    } 
                }
            }
            sb.Append(" ");
            if (type != null) {
                sb.Append("at ");
                if (qualifiedTypeErrors) {
                    sb.Append(type.@namespace);
                    sb.Append('.');
                }
                sb.Append(type.name);
                sb.Append(" > ");
            }
            sb.Append(path);
            sb.Append(", pos: ");
            sb.Append(pos);
            return sb.ToString();
        }
    }
}