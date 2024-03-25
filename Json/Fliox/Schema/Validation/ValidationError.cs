// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Fliox.Schema.Validation
{
    internal readonly struct ValidationError
    {
        internal readonly   string              msg;
        private  readonly   string              value;
        private  readonly   bool                isString;
        private  readonly   string              expect;
        private  readonly   string              expectNamespace;
        private  readonly   ValidationTypeDef   typeDef;
        private  readonly   string              path;
        private  readonly   int                 pos;

        public  override    string              ToString() => msg == null ? "no error" : AsString(new StringBuilder(), false);

        internal ValidationError (string msg, string value, bool isString, string expect, string expectNamespace, ValidationTypeDef typeDef, string path, int pos) {
            this.msg            = msg;
            this.value          = value;
            this.isString       = isString;
            this.expect         = expect;
            this.expectNamespace= expectNamespace;
            this.typeDef        = typeDef;
            this.path           = path;
            this.pos            = pos;
        }
        
        internal ValidationError (string msg, string value, bool isString, ValidationTypeDef typeDef, string path, int pos) {
            this.msg            = msg;
            this.value          = value;
            this.isString       = isString;
            this.expect         = null;
            this.expectNamespace= null;
            this.typeDef        = typeDef;
            this.path           = path;
            this.pos            = pos;
        }
        
        internal string AsString (StringBuilder sb, bool qualifiedTypeErrors) {
            sb.Clear();
            sb.Append(msg);
            if (value != null) {
                if (expect != null) {
                    sb.Append(" was: ");
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
            sb.Append(' ');
            if (typeDef != null) {
                sb.Append("at ");
                if (qualifiedTypeErrors) {
                    sb.Append(typeDef.@namespace);
                    sb.Append('.');
                }
                sb.Append(typeDef.name);
                sb.Append(" > ");
            }
            sb.Append(path);
            sb.Append(", pos: ");
            sb.Append(pos);
            return sb.ToString();
        }
    }
}