// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
	using Str128 = Unity.Collections.FixedString128;
#else
	using Str32 = System.String;
	using Str128 = System.String;
#endif

namespace Friflo.Json.Burst
{
	public struct SkipInfo {
		public int arrays;
		public int booleans;
		public int floats;
		public int integers;
		public int nulls;
		public int objects;
		public int strings;

		public int Sum => arrays + booleans + floats + integers + nulls + objects + strings;
	}
	
	public partial struct JsonParser : IDisposable
	{
		private 			int					pos;
		private 			ByteArray			buf;
		private 			int					bufEnd;
		private				int					stateLevel;
		private				int					startPos;
	//	public				JsonEvent			lastEvent { get ; private set; }

		private 			ValueArray<int>		state;
		private 			ValueArray<int>		pathPos;  	// used for current path
		private 			ValueArray<int>		arrIndex;  	// used for current path
		public				ErrorCx				error;
		private				ValueError			valueError;
	
		public				bool				boolValue;
		public				Bytes				key;
		private				Bytes				path;  // used for current path
		private				Bytes				misc;
		public				Bytes				value;
		
		private				ValueFormat			format;
		private				ValueParser			valueParser;

		private				String32			@true;
		private				String32			@false;
		private				String32			@null;

		public				bool				isFloat;
		public				SkipInfo			skipInfo;
		
	
		private const int 	ExpectMember =			0;
		private const int 	ExpectMemberFirst =		1;
	
		private const int 	ExpectElement =			2;
		private const int 	ExpectElementFirst =	3;
	
		private const int 	ExpectRoot =			4;
		private const int 	Finished =				6;

	
		public String128	GetError()	{	return error.Msg;		}
		public int			GetLevel()	{	return stateLevel;		}

		public void Error (Str32 module, Str128 msg) {
			misc.Clear();
			AppendPath(ref misc);
			int position = pos - startPos;
			var err = new String128($"{module} error - {msg} path: {misc.ToFixed128()} at position: {position}");
			error.Error(err, pos);
		}
		
		private JsonEvent SetError (Str128 msg)
		{
			Error("JsonParser", msg);
			return JsonEvent.Error;
		}
		
		private bool SetErrorFalse (Str128 msg)
		{
			Error("JsonParser", msg);
			return false;
		}
	
		public String GetPath()
		{
			misc.Clear();
			AppendPath(ref misc);
			return misc.ToString();
		}
		
		public override string ToString() {
			return $"{{ path: \"{GetPath()}\", pos: {pos} }}";
		}
		
		public void AppendPath(ref Bytes str)
		{
			int lastPos = 0;
			for (int n = 1; n <= stateLevel; n++)
			{
				switch (state[n])
				{
				case ExpectMember:
					if (n > 1)
						str.AppendChar('.');
                    str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos[n]);
                    break;
				case ExpectMemberFirst:
					str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos[n]);
					break;
				case ExpectElement:
				case ExpectElementFirst:
					if (arrIndex[n] != -1)
					{
						str.AppendChar('[');
						format.AppendInt(ref str, arrIndex[n]);
						str.AppendChar(']');
					}
					else
						str.AppendFixed32("[]");
					break;
				}
			}
		}

		private void InitContainers() {
			if (state.IsCreated())
				return;
			state =	 new ValueArray<int>(32);
			pathPos = new ValueArray<int>(32);
			arrIndex = new ValueArray<int>(32);
			key.InitBytes(32);
			path.InitBytes(32);
			misc.InitBytes(32); 
			value.InitBytes(32);
			format.InitTokenFormat();
			@true = new String32("true");
			@false = new String32("false");
			@null = new String32("null");
			valueParser.InitValueParser();
		}

		public void Dispose() {
			valueParser.Dispose();
			format.Dispose();
			value.Dispose();
			misc.Dispose();
			path.Dispose();
			key.Dispose();
			if (arrIndex.IsCreated())	arrIndex.Dispose();
			if (pathPos.IsCreated())	pathPos.Dispose();
			if (state.IsCreated())		state.Dispose();
		}
		
		public void InitParser(Bytes bytes) {
			InitParser (bytes.buffer, bytes.Start, bytes.Len);
		}

		public void InitParser(ByteArray bytes, int offset, int len) {
			InitContainers();
			stateLevel = 0;
			state[0] = ExpectRoot;

			this.pos = offset;
			this.startPos = offset;
			this.buf = bytes;
			this.bufEnd = offset + len;
			skipInfo = default(SkipInfo);
			error.Clear();
		}

		/* public JsonEvent NextEvent() {
			JsonEvent ev = nextEvent();
			lastEvent = ev;
			return ev;
		} */

		public JsonEvent NextEvent()
		{
			int c = ReadWhiteSpace();
			int curState = state[stateLevel];
			switch (curState)
			{
    		case ExpectMember:
    		case ExpectMemberFirst:
				switch (c)
				{
					case ',':
						if (curState == ExpectMemberFirst)
							return SetError ("unexpected member separator");
						c = ReadWhiteSpace();
						if (c != '"')
							return SetError ($"expect key. Found {(char)c}");
						break;
		            case '}':
		            	stateLevel--;
		            	return JsonEvent.ObjectEnd;
		            case  -1:
		            	return SetError("unexpected EOF - expect key");
		            case '"':
		            	if (curState == ExpectMember)
		            		return SetError ("expect member separator");
		            	break;
		            default:
		            	return SetError($"unexpected character: {(char)c} - expect key");
				}
	        	// case: c == '"'
				state[stateLevel] = ExpectMember;
	        	if (!ReadString(ref key))
	        		return JsonEvent.Error;
	        	// update current path
	        	path.SetEnd(pathPos[stateLevel-1]);  // "Clear"
	        	path.AppendBytes(ref key);
	        	pathPos[stateLevel] = path.End;
	        	//
	        	c = ReadWhiteSpace();
	       		if (c != ':')
	       			return SetError ($"Expected ':' after key. Found: {(char)c}");
	    		c = ReadWhiteSpace();
	            break;
            
    		case ExpectElement:
    		case ExpectElementFirst:
				arrIndex[stateLevel]++;
				if (c == ']')
				{
					stateLevel--;
					return JsonEvent.ArrayEnd;
				}
    			if (curState == ExpectElement)
    			{
	    			if (c != ',')
	    				return SetError($"expected array separator ','. Found: {(char)c}");
    				c = ReadWhiteSpace();
    			}
    			else
    				state[stateLevel] = ExpectElement;
    			break;
    		
			case ExpectRoot:
        		switch (c)
        		{
					case '{':
						pathPos[stateLevel+1] = pathPos[stateLevel];
	            		state[++stateLevel] = ExpectMemberFirst;
	            		return JsonEvent.ObjectStart;
					case '[':
						pathPos[stateLevel+1] = pathPos[stateLevel];
	            		state[++stateLevel] = ExpectElementFirst;
						arrIndex[stateLevel] = -1;
	            		return JsonEvent.ArrayStart;
					case -1:
	            		state[stateLevel] = Finished;
	            		return JsonEvent.EOF;
					default:
	        			return SetError($"Expected '{{', '[' or EOF at root level. Found: {(char)c}");
        		}
			case Finished:
        		return SetError("Parser already finished");
			}
        
			// ---- read value of key/value pairs or array items ---
			switch (c)
			{
				case '"':
            		if (ReadString(ref value))
            			return JsonEvent.ValueString;
            		return JsonEvent.Error;
				case '{':
					pathPos[stateLevel+1] = pathPos[stateLevel];
            		state[++stateLevel] = ExpectMemberFirst;
            		return JsonEvent.ObjectStart;
				case '[':
					pathPos[stateLevel+1] = pathPos[stateLevel];
            		state[++stateLevel] = ExpectElementFirst;
					arrIndex[stateLevel] = -1;
            		return JsonEvent.ArrayStart;
				case '0':	case '1':	case '2':	case '3':	case '4':
				case '5':	case '6':	case '7':	case '8':	case '9':
				case '-':	case '+':	case '.':
            		if (ReadNumber())
            			return JsonEvent.ValueNumber;
            		return JsonEvent.Error;
				case 't':
            		if (!ReadKeyword(ref @true ))
            			return JsonEvent.Error;
            		boolValue= true;
            		return JsonEvent.ValueBool;
				case 'f':
            		if (!ReadKeyword(ref @false))
            			return JsonEvent.Error;
            		boolValue= false;
            		return JsonEvent.ValueBool;
				case 'n':
            		if (!ReadKeyword(ref @null))
            			return JsonEvent.Error;
            		return JsonEvent.ValueNull;
				case  -1:
            		return SetError("unexpected EOF while reading value");
				default:
	        		return SetError($"unexpected character: {(char)c} while reading value");
			}
			// unreachable
		}

		private int ReadWhiteSpace()
		{
			// using locals improved performance
			ref var b = ref buf.array;
			int p = pos;
			int end = bufEnd;
			for (; p < end; )
			{
				int c = b[p++];
				if (c > ' ') {
					pos = p;
            		return c;
				}
				if (c != ' '	&&
            		c != '\t'	&&
            		c != '\n'	&&
            		c != '\r') {
					pos = p;
            		return c;
				}
			}
			pos = p;
			return -1;
		}
	
		private bool ReadNumber ()
		{
			isFloat = false;
			int start = pos - 1;
			for (; pos < bufEnd; pos++)
			{
				int c = buf.array[pos];
				switch (c)
				{
				case '0':	case '1':	case '2':	case '3':	case '4':
				case '5':	case '6':	case '7':	case '8':	case '9':
				case '-':	case '+':
					continue;
				case '.':	case 'e':	case 'E':
					isFloat = true;
            		continue;
				}
				switch (c) {
					case ',': case '}': case ']':
					case ' ': case '\r': case '\n': case '\t':
						value.Clear();
						value.AppendArray(ref buf, start, pos);
						return true;
				}
				return SetErrorFalse($"unexpected character {(char)c} while reading number");
			}
			return SetErrorFalse("unexpected EOF while reading number");
		}
	
		private bool ReadString(ref Bytes token)
		{
			// using locals improved performance
			ref var b = ref buf.array;
			int p = pos;
			int end = bufEnd;
			token.Clear();
			int start = p;
			for (; p < end; p++)
			{
				int c = b[p];
				if (c == '\"')
				{
            		token.AppendArray(ref buf, start, p++);
                    pos = p;
					return true;
				}
				if (c == '\r' || c == '\n')
					return SetErrorFalse("unexpected line feed while reading string");
				if (c == '\\')
				{
            		token.AppendArray(ref buf, start, p);
            		if (++p >= end)
            			break;
            		c = b[p];
            		switch (c)
            		{
					case '"':	token.AppendChar('"');	break;
					case '\\':	token.AppendChar('\\');	break;
					case '/':	token.AppendChar('/');	break;
					case 'b':	token.AppendChar('\b');	break;
					case 'f':	token.AppendChar('\f');	break;
					case 'r':	token.AppendChar('\r');	break;
					case 'n':	token.AppendChar('\n');	break;
					case 't':	token.AppendChar('\t');	break;                	
					case 'u':
						pos = p;
						if (!ReadUnicode(ref token))
							return false;
						p = pos;
						break;
            		}
            		start = p + 1;
				}
			}
			pos = p;
			return SetErrorFalse("unexpected EOF while reading string");
		}
	
		private bool ReadUnicode (ref Bytes tokenBuffer) {
			ref Bytes token = ref tokenBuffer;
			pos += 4;
			if (pos >= bufEnd)
				return SetErrorFalse("Expect 4 hex digits after '\\u' in value");

			int d1 = Digit2Int(buf.array[pos - 3]);
			int d2 = Digit2Int(buf.array[pos - 2]);
			int d3 = Digit2Int(buf.array[pos - 1]);
			int d4 = Digit2Int(buf.array[pos - 0]);
			if (d1 == -1 || d2 == -1 || d3 == -1 || d4 == -1)
				return SetErrorFalse("Invalid hex digits after '\\u' in value");

			int uni = d1 << 12 | d2 << 8 | d3 << 4 | d4;
		
			// UTF-8 Encoding
			tokenBuffer.EnsureCapacity(4);
			ref var str = ref token.buffer.array;
			int i = token.End;
			if (uni < 0x80)
			{
				str[i] =	(byte)uni;
				token.SetEnd(i + 1);
				return true;
			}
			if (uni < 0x800)
			{
				str[i]   =	(byte)(m_11oooooo | (uni >> 6));
				str[i+1] =	(byte)(m_1ooooooo | (uni 		 & m_oo111111));
				token.SetEnd(i + 2);
				return true;
			}
			if (uni < 0x10000)
			{
				str[i]   =	(byte)(m_111ooooo |  (uni >> 12));
				str[i+1] =	(byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
				str[i+2] =	(byte)(m_1ooooooo |  (uni        & m_oo111111));
				token.SetEnd(i + 3);
				return true;
			}
			str[i]   =		(byte)(m_1111oooo |  (uni >> 18));
			str[i+1] =		(byte)(m_1ooooooo | ((uni >> 12) & m_oo111111));
			str[i+2] =		(byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
			str[i+3] =		(byte)(m_1ooooooo |  (uni        & m_oo111111));
			token.SetEnd(i + 4);
			return true;
		}
	
		private static readonly int 	m_1ooooooo = 0x80;
		private static readonly int 	m_11oooooo = 0xc0;
		private static readonly int 	m_111ooooo = 0xe0;
		private static readonly int 	m_1111oooo = 0xf0;
	
		private static readonly int 	m_oo111111 = 0x3f;
	
		private static int Digit2Int (int c)
		{
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}
	
		private bool ReadKeyword (ref String32 keyword)
		{
			int start = pos - 1;
			ref var b = ref buf.array;
			for (; pos < bufEnd; pos++)
			{
				int c = b[pos];
				if ('a' <= c && c <= 'z')
					continue;
				break;
			}
			int len = pos - start;
			int keyLen = keyword.value.Length;
			if (len != keyLen) {
				value.Clear();
				value.AppendArray(ref buf, start, pos);
				return SetErrorFalse($"invalid value: {value.ToFixed32()}");
			}

			for (int n = 1; n < len; n++)
			{
				if (keyword.value[n] != b[start + n]) {
					value.Clear();
					value.AppendArray(ref buf, start, pos);
					return SetErrorFalse($"invalid value: {value.ToFixed32()}");
				}
			}
			return true;
		}

		public bool SkipTree()
		{
	        int curState = state[stateLevel];
	        switch (curState)
	        {
	        case ExpectMember:
	        case ExpectMemberFirst:
	        	return SkipObject();
	        case ExpectElement:
	        case ExpectElementFirst:
	        	return SkipArray();
	        default:
	        	return SetErrorFalse("unexpected state");
	        }
		}
		
		private bool SkipObject()
		{
			skipInfo.objects++;
			while (true)
			{
				JsonEvent ev = NextEvent();
				switch (ev)
				{
				case JsonEvent. ValueString:
					skipInfo.strings++;
					break;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					break;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					break;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					break;
				case JsonEvent. ObjectStart:
					if (!SkipObject())
						return false;
					break;
				case JsonEvent. ArrayStart:
					if(!SkipArray())
						return false;
					break;
				case JsonEvent. ObjectEnd:
					return true;
				case JsonEvent. Error:
					return false;
				default:
					return SetErrorFalse($"unexpected state: {ev}");
				}
			}
		}
		
		private bool SkipArray()
		{
			skipInfo.arrays++;
			while (true)
			{
				JsonEvent ev = NextEvent();
				switch (ev)
				{
				case JsonEvent. ValueString:
					skipInfo.strings++;
					break;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					break;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					break;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					break;
				case JsonEvent. ObjectStart:
					if (!SkipObject())
						return false;
					break;
				case JsonEvent. ArrayStart:
					if(!SkipArray())
						return false;
					break;
				case JsonEvent. ArrayEnd:
					return true;
				case JsonEvent. Error:
					return false;
				default:
					return SetErrorFalse($"unexpected state: {ev}");
				}
			}
		}
		
		public bool SkipEvent (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ArrayStart:
				case JsonEvent.ObjectStart:
					return SkipTree();
				case JsonEvent.ArrayEnd:
				case JsonEvent.ObjectEnd:
					return false;
				case JsonEvent. ValueString:
					skipInfo.strings++;
					return true;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					return true;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					return true;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					return true;
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true; // unreachable
		}
		
		public bool ContinueObject (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ArrayEnd:
					return SetErrorFalse("Unexpected JsonEvent.ArrayEnd in ContinueObject");
				case JsonEvent.ObjectEnd:
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true;
		}
		
		public bool ContinueArray (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ObjectEnd:
					return SetErrorFalse("Unexpected JsonEvent.ObjectEnd in ContinueArray");
				case JsonEvent.ArrayEnd:
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true;
		}

		public double ValueAsDouble(out bool success) {
			double result = valueParser.ParseDouble(ref value, ref valueError, out success);
			if (!success)
				SetErrorFalse(valueError.GetError().value);
			return result;
		}
		
		public double ValueAsDoubleFast(out bool success) {
			double result = valueParser.ParseDoubleFast(ref value, ref valueError, out success);
			if (!success) 
				SetErrorFalse(valueError.GetError().value);
			return result;
		}
		
		public float ValueAsFloat(out bool success) {
			double result = valueParser.ParseDoubleFast(ref value, ref valueError, out success);
			if (!success) 
				SetErrorFalse(valueError.GetError().value);
			return (float)result;
		}
		
		public long ValueAsLong(out bool success) {
			long result = valueParser.ParseLong(ref value, ref valueError, out success);
			if ( !success)
				SetErrorFalse(valueError.GetError().value);
			return result;
		}
		
		public int ValueAsInt(out bool success) {
			int result = valueParser.ParseInt(ref value, ref valueError, out success);
			if (!success)
				SetErrorFalse(valueError.GetError().value);
			return result;
		}

	}
}