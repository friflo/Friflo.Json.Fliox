// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        private void SetError(MsgReaderState error, MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            var sb  = StopReader(error, type, cur);
            sb.Append($"MessagePack error - {MsgPackUtils.Error(error)}. was: {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        
        private void SetRangeError(MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            var sb  = StopReader(MsgReaderState.RangeError, type, cur);
            CreateErrorMessage(sb);
            SetMessage(sb, cur);
        }
        
        private void SetEofError(int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            var sb  = StopReader(MsgReaderState.UnexpectedEof, MsgFormat.root, cur);
            var msg = MsgPackUtils.Error(MsgReaderState.UnexpectedEof);
            sb.Append($"MessagePack error - {msg}.");
            SetMessage(sb, cur);
        }
        
        private void SetEofErrorType(MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            var sb  = StopReader(MsgReaderState.UnexpectedEof, type, cur);
            var msg = MsgPackUtils.Error(MsgReaderState.UnexpectedEof);
            sb.Append($"MessagePack error - {msg}. type: {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        
        // ----------------------------------------- utils -----------------------------------------
        private void CreateErrorMessage(StringBuilder sb) {
            sb.Append("MessagePack error - ");
            sb.Append(MsgPackUtils.Error(state));
            sb.Append('.');
            sb.Append(" was: ");
            if (state == MsgReaderState.RangeError) {
                MsgPackUtils.AppendValue(sb, data, errorType, errorPos);
                sb.Append(' ');
            }
            sb.Append($"{MsgPackUtils.Name(errorType)}(0x{(int)errorType:X})");
        }
        
        private StringBuilder StopReader(MsgReaderState state, MsgFormat type, int cur) {
            this.state  = state;
            pos         = MsgError;
            errorType   = type;
            errorPos    = cur;
            return new StringBuilder();
        }
        
        private void SetMessage(StringBuilder sb, int cur) {
            sb.Append(" pos: ");
            sb.Append(cur);
            if (keyName == null) {
                sb.Append(" (root)");
            } else {
                var key = MsgPackUtils.SpanToString(keyName);
                sb.Append(" - last key: '");
                sb.Append(key);
                sb.Append('\'');
            }
            error   = sb.ToString();
        }
    }
}