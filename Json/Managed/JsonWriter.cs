// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed
{
	public class JsonWriter : IDisposable
	{
		private readonly	PropType.Cache typeCache;
		private				Bytes		bytes;
		private				ValueFormat	format;
		private				Bytes		strBuf;

		private				Bytes		_null = new Bytes("null");
		private				Bytes		_type = new Bytes("\"$type\":\"");

		public			ref Bytes Output => ref bytes;

		public JsonWriter(PropType.Store store) {
			typeCache = new PropType.Cache(store);
		}
		
		public void Dispose() {
			_null.Dispose();
			_type.Dispose();
			format.Dispose();
			strBuf.Dispose();
			bytes.Dispose();
		}

		public void Write(Object obj) {
			bytes.InitBytes(128);
			strBuf.InitBytes(128);
			format.InitTokenFormat();
			PropType type = typeCache.Get(obj.GetType());
			WriteObject(type, obj);
		}

		private void WriteKey(PropField field) {
			bytes.AppendChar('\"');
			field.AppendName(ref bytes);
			bytes.AppendString("\":");
		}

		private void WriteString(String str) {
			bytes.AppendChar('\"');
			strBuf.Clear();
			strBuf.FromString(str);
			int len = strBuf.Len;
			ref ByteArray buf = ref strBuf.buffer;
			for (int n = 0; n < len; n++) {
				char c = (char) buf.array[n];
				switch (c) {
					case '"':
						bytes.AppendString("\\\"");
						break;
					case '\\':
						bytes.AppendString("\\\\");
						break;
					case '\b':
						bytes.AppendString("\\b");
						break;
					case '\f':
						bytes.AppendString("\\f");
						break;
					case '\r':
						bytes.AppendString("\\r");
						break;
					case '\n':
						bytes.AppendString("\\n");
						break;
					case '\t':
						bytes.AppendString("\\t");
						break;
					default:
						bytes.AppendChar(c);
						break;
				}
			}

			bytes.AppendChar('\"');
		}

		private void WriteObject(PropType type, Object obj) {
			bool firstMember = true;
			bytes.AppendChar('{');
			Type objType = obj.GetType();
			if (type.nativeType != objType) {
				type = typeCache.Get(objType);
				firstMember = false;
				bytes.AppendBytes(ref _type);
				Bytes subType = type.typeName;
				if (subType.buffer.IsCreated())
					bytes.AppendBytes(ref subType);
				else
					throw new FrifloException("Serializing derived types must be registered: " + objType.Name);
				bytes.AppendChar('\"');
			}

			PropField[] fields = type.propFields.fieldsSerializable;

			for (int n = 0; n < fields.Length; n++) {
				if (firstMember)
					firstMember = false;
				else
					bytes.AppendChar(',');
				PropField field = fields[n];
				switch (field.type) {
					case SimpleType.ID.String:
						WriteKey(field);
						String val = field.GetString(obj);
						if (val != null)
							WriteString(val);
						else
							bytes.AppendBytes(ref _null);
						break;
					case SimpleType.ID.Long:
						WriteKey(field);
						format.AppendLong(ref bytes, field.GetLong(obj));
						break;
					case SimpleType.ID.Integer:
						WriteKey(field);
						format.AppendInt(ref bytes, field.GetInt(obj));
						break;
					case SimpleType.ID.Short:
						WriteKey(field);
						format.AppendInt(ref bytes, field.GetInt(obj));
						break;
					case SimpleType.ID.Byte:
						WriteKey(field);
						format.AppendInt(ref bytes, field.GetInt(obj));
						break;
					case SimpleType.ID.Bool:
						WriteKey(field);
						format.AppendBool(ref bytes, field.GetBool(obj));
						break;
					case SimpleType.ID.Double:
						WriteKey(field);
						format.AppendDbl(ref bytes, field.GetDouble(obj));
						break;
					//													bytes.Append(field.GetString(obj));		break;	// precise conversion
					case SimpleType.ID.Float:
						WriteKey(field);
						format.AppendFlt(ref bytes, field.GetFloat(obj));
						break;
					// 													bytes.Append(field.GetString(obj));		break;	// precise conversion
					case SimpleType.ID.Object:
						WriteKey(field);
						Object child = field.GetObject(obj);
						if (child == null) {
							bytes.AppendBytes(ref _null);
						}
						else {
							PropCollection collection = field.collection;
							if (collection == null)
								WriteObject(field.GetFieldPropType(typeCache), child);
							else
								WriteCollection(collection, child);
						}

						break;
					default:
						throw new FrifloException("invalid field type: " + field.type);
				}
			}

			bytes.AppendChar('}');
		}

		private void WriteCollection(PropCollection collection, Object col) {
			Type typeInterface = collection.typeInterface;
			if (typeInterface == typeof(Array)) {
				bytes.AppendChar('[');
				switch (collection.ID) {
					case SimpleType.ID.String:
						WriteArrayString((String[]) col);
						break;
					case SimpleType.ID.Long:
						WriteArrayLong((long[]) col);
						break;
					case SimpleType.ID.Integer:
						WriteArrayInt((int[]) col);
						break;
					case SimpleType.ID.Short:
						WriteArrayShort((short[]) col);
						break;
					case SimpleType.ID.Byte:
						WriteArrayByte((byte[]) col);
						break;
					case SimpleType.ID.Bool:
						WriteArrayBool((bool[]) col);
						break;
					case SimpleType.ID.Double:
						WriteArrayDouble((double[]) col);
						break;
					case SimpleType.ID.Float:
						WriteArrayFloat((float[]) col);
						break;
					case SimpleType.ID.Object:
						if (collection.elementPropType == null)
							collection.elementPropType = typeCache.Get(collection.elementType);
						WriteArrayObject(collection.elementPropType, (Object[]) col);
						break;
					default:
						throw new FrifloException("unsupported array type: " + collection.ID);
				}

				bytes.AppendChar(']');
			}
			else if (typeInterface == typeof(IList<>)) {
				WriteList(collection, (IList) col);
			}
			else if (typeInterface == typeof(IDictionary<,>)) {
				WriteMap(collection, (IDictionary) col);
			}
			else {
				throw new FrifloException("Unsupported collection: " + typeInterface);
			}
		}

		private void WriteList(PropCollection collection, IList list) {
			bytes.AppendChar('[');
			if (collection.elementPropType == null)
				collection.elementPropType = typeCache.Get(collection.elementType);
			PropType itemType = collection.elementPropType;
			for (int n = 0; n < list.Count; n++) {
				if (n > 0) bytes.AppendChar(',');
				Object item = list[n];
				if (item != null) {
					switch (collection.ID) {
						case SimpleType.ID.Object:
							WriteObject(itemType, item);
							break;
						case SimpleType.ID.String:
							WriteString((String) item);
							break;
						default:
							throw new FrifloException("List element type not supported: " + collection.elementType.Name);
					}
				}
				else
					bytes.AppendBytes(ref _null);
			}

			bytes.AppendChar(']');
		}

		private void WriteMap(PropCollection collection, IDictionary map) {
			bytes.AppendChar('{');
			int n = 0;
			if (collection.elementType == typeof(String)) {
				// Map<String, String>
				// @SuppressWarnings("unchecked")
				IDictionary<String, String> strMap = (IDictionary<String, String>) map;
				foreach (KeyValuePair<String, String> entry in strMap) {
					if (n++ > 0) bytes.AppendChar(',');
					WriteString(entry.Key);
					bytes.AppendChar(':');
					String value = entry.Value;
					if (value != null)
						WriteString(value);
					else
						bytes.AppendBytes(ref _null);
				}
			}
			else {
				// Map<String, Object>
				if (collection.elementPropType == null)
					collection.elementPropType = typeCache.Get(collection.elementType);
				PropType itemType = collection.elementPropType;
				foreach (DictionaryEntry entry in map) {
					if (n++ > 0) bytes.AppendChar(',');
					WriteString((String) entry.Key);
					bytes.AppendChar(':');
					Object value = entry.Value;
					if (value != null)
						WriteObject(itemType, value);
					else
						bytes.AppendBytes(ref _null);
				}
			}

			bytes.AppendChar('}');
		}

		/* ----------------------------------------- array writers -------------------------------------------- */
		private void WriteArrayString(String[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				String item = arr[n];
				if (item != null)
					WriteString(item);
				else
					bytes.AppendBytes(ref _null);
			}
		}

		private void WriteArrayLong(long[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendLong(ref bytes, arr[n]);
			}
		}

		private void WriteArrayInt(int[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendInt(ref bytes, arr[n]);
			}
		}

		private void WriteArrayShort(short[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendInt(ref bytes, arr[n]);
			}
		}

		private void WriteArrayByte(byte[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendInt(ref bytes, arr[n]);
			}
		}

		private void WriteArrayBool(bool[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendBool(ref bytes, arr[n]);
			}
		}

		private void WriteArrayDouble(double[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendDbl(ref bytes, arr[n]);
				//	bytes.Append( arr[n] .ToString());	// precise conversion
			}
		}

		private void WriteArrayFloat(float[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				format.AppendFlt(ref bytes, arr[n]);
				//  bytes.Append( arr[n] .ToString());	// precise conversion
			}
		}

		private void WriteArrayObject(PropType elementPropType, Object[] arr) {
			for (int n = 0; n < arr.Length; n++) {
				if (n > 0) bytes.AppendChar(',');
				if (arr[n] == null)
					bytes.AppendBytes(ref _null);
				else
					WriteObject(elementPropType, arr[n]);
			}
		}
	}
}