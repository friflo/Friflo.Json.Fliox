using System;
using System.IO;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper
{
    // ReSharper disable once InconsistentNaming
    public static class JSON
    {
        public static readonly TypeStore TypeStore = new TypeStore();
        
        // --------------- Bytes ---------------
        // --- Read()
        public static T Read<T>(Bytes utf8Bytes) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.Read<T>(utf8Bytes);
            }
        }
        
        public static object ReadObject(Bytes utf8Bytes, Type type) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadObject(utf8Bytes, type);
            }
        }

        // --- ReadTo()
        public static T ReadTo<T>(Bytes utf8Bytes, T obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadTo(utf8Bytes, obj);
            }
        }

        public static object ReadToObject(Bytes utf8Bytes, object obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadToObject(utf8Bytes, obj);
            }
        }
        
        // --------------- Stream ---------------
        // --- Read()
        public static T Read<T>(Stream utf8Stream) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.Read<T>(utf8Stream);
            }
        }
        
        public static object ReadObject(Stream utf8Stream, Type type) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadObject(utf8Stream, type);
            }
        }

        // --- ReadTo()
        public static T ReadTo<T>(Stream utf8Stream, T obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadTo(utf8Stream, obj);
            }
        }

        public static object ReadToObject(Stream utf8Stream, object obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadToObject(utf8Stream, obj);
            }
        }
        
        // --------------- string ---------------
        // --- Read()
        public static T Read<T>(string json) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.Read<T>(json);
            }
        }
        
        public static object ReadObject(string json, Type type) {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadObject(json, type);
            }
        }

        // --- ReadTo()
        public static T ReadTo<T>(string json, T obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadTo(json, obj);
            }
        }

        public static object ReadToObject(string json, object obj)  {
            using (var reader = new JsonReader(TypeStore)) {
                return reader.ReadToObject(json, obj);
            }
        }
        
    }
}