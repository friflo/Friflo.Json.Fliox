// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        private void SetError(MsgReaderState error, MsgFormat type, int cur) {
            if (state != Ok) {
                return;
            }
            StopReader(error, type, cur);
        }
        
        private void SetRangeError(MsgReaderState expect, MsgFormat type, int cur) {
            if (state != Ok) {
                return;
            }
            StopReader(expect | RangeError, type, cur);
        }
        
        internal void SetEofError() {
            if (state != Ok) {
                return;
            }
            StopReader(UnexpectedEof, MsgFormat.root, pos);
        }
        
        private void SetEofError(int cur) {
            if (state != Ok) {
                return;
            }
            StopReader(UnexpectedEof, MsgFormat.root, cur);
        }
        
        private void SetEofErrorType(MsgFormat type, int cur) {
            if (state != Ok) {
                return;
            }
            StopReader(UnexpectedEof, type, cur);
        }
        
        // ----------------------------------------- utils -----------------------------------------
        private void StopReader(MsgReaderState state, MsgFormat type, int cur) {
            this.state  = state;
            pos         = MsgError;
            errorType   = type;
            errorPos    = cur;
        }
        
        public string CreateErrorMessage(StringBuilder sb)
        {
            if (state == Ok) {
                return null;
            }
            var isRangeError = (state & RangeError) != 0;
            sb ??= new StringBuilder();
            sb.Append("MessagePack error - ");
            if (isRangeError) {
                sb.Append("value out of range / ");
                sb.Append(MsgPackUtils.Error(state));
            } else {
                sb.Append(MsgPackUtils.Error(state));
            }
            sb.Append('.');
            if (errorType != MsgFormat.root) {
                sb.Append(" was: ");
                if (isRangeError) {
                    MsgPackUtils.AppendValue(sb, data, errorType, errorPos);
                    sb.Append(' ');
                }
                sb.Append($"{MsgPackUtils.Name(errorType)}(0x{(int)errorType:X})");
            }
            sb.Append(" pos: ");
            sb.Append(errorPos);
            if (keyName == null) {
                sb.Append(" (root)");
            } else {
                var key = MsgPackUtils.SpanToString(keyName);
                sb.Append(" - last key: '");
                sb.Append(key);
                sb.Append('\'');
            }
            return sb.ToString();
        }
    }
}