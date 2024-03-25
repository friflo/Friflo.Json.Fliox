// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    // -------------------------------------------- RefKey -----------------------------------------------
    internal abstract class KeyConverter {
        private static readonly   Dictionary<Type, KeyConverter> Map = new Dictionary<Type, KeyConverter>();

        internal static KeyConverter<TKey> GetConverter<TKey> () {
            var keyType = typeof(TKey);
            if (Map.TryGetValue(keyType, out KeyConverter id)) {
                return (KeyConverter<TKey>)id;
            }
            var result  = CreateRefKey<TKey>();
            Map[keyType]   = result;
            return (KeyConverter<TKey>)result;
        }

        private static KeyConverter CreateRefKey<TKey> () {
            var keyType = typeof (TKey);
            if (keyType == typeof(string))      return new KeyConverterString       ();
            if (keyType == typeof(ShortString)) return new KeyConverterShortString  ();
            if (keyType == typeof(Guid))        return new KeyConverterGuid         ();
            if (keyType == typeof(Guid?))       return new KeyConverterGuidNull     ();
            if (keyType == typeof(int))         return new KeyConverterInt          ();
            if (keyType == typeof(int?))        return new KeyConverterIntNull      ();
            if (keyType == typeof(long))        return new KeyConverterLong         ();
            if (keyType == typeof(long?))       return new KeyConverterLongNull     ();
            if (keyType == typeof(short))       return new KeyConverterShort        ();
            if (keyType == typeof(short?))      return new KeyConverterShortNull    ();
            if (keyType == typeof(byte))        return new KeyConverterByte         ();
            if (keyType == typeof(byte?))       return new KeyConverterByteNull     ();
            if (keyType == typeof(JsonKey))     return new KeyConverterJsonKey      ();
            var msg = UnsupportedTypeMessage(keyType);
            throw new InvalidOperationException(msg);
        }

        private static string UnsupportedTypeMessage(Type keyType) {
            return $"unsupported key Type: {keyType.Name}";
        }
    }

    // ------------------------------------------ RefKey<TKey> ------------------------------------------
    internal abstract class KeyConverter<TKey> : KeyConverter {
        internal abstract   JsonKey KeyToId (in TKey key);
        internal abstract   TKey    IdToKey (in JsonKey key);
        
        internal virtual    bool    IsKeyNull (TKey key) => false;
    }
}