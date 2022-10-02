// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;

namespace Friflo.Json.Fliox.Hub.Client
{
    public static class EntityUtils<TKey,T> where T : class
    {
        private static  readonly    EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static  readonly    KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();
        
        public  static  TKey    IdToKey(in JsonKey id)                  => KeyConvert.IdToKey(id);
        public  static  JsonKey KeyToId(TKey key)                       => KeyConvert.KeyToId(key);

        public  static  void    SetEntityId (T entity, in JsonKey id)   => EntityKeyTMap.SetId(entity, id);
        public  static  JsonKey GetEntityId (T entity)                  => EntityKeyTMap.GetId(entity);

        public  static  void    SetEntityKey(T entity, TKey key)        => EntityKeyTMap.SetKey(entity, key);
        public  static  TKey    GetEntityKey(T entity)                  => EntityKeyTMap.GetKey(entity);
    }
}