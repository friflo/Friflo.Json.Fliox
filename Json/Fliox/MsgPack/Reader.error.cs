// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Friflo.Json.Fliox.MsgPack
{

    public ref partial struct MsgReader
    {
        private void SetTypeError(string expect, MsgFormat type, int cur) {
            if (pos == MsgError) {
                return;
            }
            var sb  = SetError();
            sb.Append($"MessagePack error - {expect}. was: {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        
        private void SetIntRangeError(MsgFormat type, long value, int cur) {
            if (pos == MsgError) {
                return;
            }
            var sb  = SetError();
            sb.Append($"MessagePack error - value out of range. was: {value} {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        
        private void SetFloatRangeError(MsgFormat type, double value, int cur) {
            if (pos == MsgError) {
                return;
            }
            var sb  = SetError();
            var val = value.ToString(NumberFormat);
            sb.Append($"MessagePack error - value out of range. was: {val} {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        private static readonly NumberFormatInfo NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
        
        private void SetEofError(int cur) {
            if (pos == MsgError) {
                return;
            }
            var sb  = SetError();
            sb.Append("MessagePack error - unexpected EOF.");
            SetMessage(sb, cur);
        }
        
        private void SetEofErrorType(MsgFormat type, int cur) {
            if (error != null) {
                return;
            }
            var sb  = SetError();
            sb.Append($"MessagePack error - unexpected EOF. type: {MsgPackUtils.Name(type)}(0x{(int)type:X})");
            SetMessage(sb, cur);
        }
        
        private StringBuilder SetError() {
            pos     = MsgError;
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