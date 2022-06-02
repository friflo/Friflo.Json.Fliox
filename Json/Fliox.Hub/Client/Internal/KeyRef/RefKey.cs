// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    // -------------------------------------------- RefKey -----------------------------------------------
    internal abstract class RefKey {
        private static readonly   Dictionary<Type, RefKey> Map = new Dictionary<Type, RefKey>();

        internal static RefKey<TKey> GetRefKey<TKey, T> () where T : class {
            var keyType = typeof(TKey);
            if (Map.TryGetValue(keyType, out RefKey id)) {
                return (RefKey<TKey>)id;
            }
            var result  = CreateRefKey<TKey, T>();
            Map[keyType]   = result;
            return (RefKey<TKey>)result;
        }

        private static RefKey CreateRefKey<TKey, T> () where T : class {
            var keyType = typeof (TKey);
            if (keyType == typeof(string))  return new RefKeyString   ();
            if (keyType == typeof(Guid))    return new RefKeyGuid     ();
            if (keyType == typeof(Guid?))   return new RefKeyGuidNull ();
            if (keyType == typeof(int))     return new RefKeyInt      ();
            if (keyType == typeof(int?))    return new RefKeyIntNull  ();
            if (keyType == typeof(long))    return new RefKeyLong     ();
            if (keyType == typeof(long?))   return new RefKeyLongNull ();
            if (keyType == typeof(short))   return new RefKeyShort    ();
            if (keyType == typeof(short?))  return new RefKeyShortNull();
            if (keyType == typeof(byte))    return new RefKeyByte     ();
            if (keyType == typeof(byte?))   return new RefKeyByteNull ();
            if (keyType == typeof(JsonKey)) return new RefKeyJsonKey  ();
            var msg = UnsupportedTypeMessage(keyType, typeof (T));
            throw new InvalidOperationException(msg);
        }

        private static string UnsupportedTypeMessage(Type keyType, Type type) {
            return $"unsupported TKey Type: Ref<{keyType.Name},{type.Name}>";
        }
    }

    // ------------------------------------------ RefKey<TKey, T> ------------------------------------------
    internal abstract class RefKey<TKey> : RefKey {
        internal abstract   JsonKey KeyToId (in TKey key);
        internal abstract   TKey    IdToKey (in JsonKey key);
        
        internal virtual    bool    IsKeyNull (TKey key) => false;
    }
}