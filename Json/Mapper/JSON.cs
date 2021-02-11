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
        public static T Read<T>(Bytes bytes) {
            var reader = new JsonReader(TypeStore);
            return reader.Read<T>(bytes);
        }
        
        public static object ReadObject(Bytes bytes, Type type) {
            var reader = new JsonReader(TypeStore);
            return reader.ReadObject(bytes, type);
        }

        // --- ReadTo()
        public static T ReadTo<T>(Bytes bytes, T obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadTo(bytes, obj);
        }

        public static object ReadToObject(Bytes bytes, object obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadToObject(bytes, obj);
        }
        
        // --------------- Stream ---------------
        // --- Read()
        public static T Read<T>(Stream stream) {
            var reader = new JsonReader(TypeStore);
            return reader.Read<T>(stream);
        }
        
        public static object ReadObject(Stream stream, Type type) {
            var reader = new JsonReader(TypeStore);
            return reader.ReadObject(stream, type);
        }

        // --- ReadTo()
        public static T ReadTo<T>(Stream stream, T obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadTo(stream, obj);
        }

        public static object ReadToObject(Stream stream, object obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadToObject(stream, obj);
        }
        
        // --------------- string ---------------
        // --- Read()
        public static T Read<T>(string json) {
            var reader = new JsonReader(TypeStore);
            return reader.Read<T>(json);
        }
        
        public static object ReadObject(string json, Type type) {
            var reader = new JsonReader(TypeStore);
            return reader.ReadObject(json, type);
        }

        // --- ReadTo()
        public static T ReadTo<T>(string json, T obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadTo(json, obj);
        }

        public static object ReadToObject(string json, object obj)  {
            var reader = new JsonReader(TypeStore);
            return reader.ReadToObject(json, obj);
        }
        
    }
}