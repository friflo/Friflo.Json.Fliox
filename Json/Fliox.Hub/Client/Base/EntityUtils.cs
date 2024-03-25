// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Utility methods for type safe key conversion and generic <typeparamref name="TKey"/> access for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Use <see cref="EntitySet{TKey,T}.Utils"/> for more convenience.
    /// The Utils property provide the same feature set without passing generic types <typeparamref name="TKey"/> and <typeparamref name="T"/>.  
    /// </remarks>
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
    
    /// <summary>
    /// Utility methods for type safe key conversion and generic <typeparamref name="TKey"/> access for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Same feature set as <see cref="EntityUtils{TKey,T}"/> but more convenient.
    /// </remarks>
    public readonly struct SetUtils<TKey,T> where T : class
    {
        private static  readonly    EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static  readonly    KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();
        
        public  TKey    IdToKey(in JsonKey id)                  => KeyConvert.IdToKey(id);
        public  JsonKey KeyToId(TKey key)                       => KeyConvert.KeyToId(key);

        public  void    SetEntityId (T entity, in JsonKey id)   => EntityKeyTMap.SetId(entity, id);
        public  JsonKey GetEntityId (T entity)                  => EntityKeyTMap.GetId(entity);

        public  void    SetEntityKey(T entity, TKey key)        => EntityKeyTMap.SetKey(entity, key);
        public  TKey    GetEntityKey(T entity)                  => EntityKeyTMap.GetKey(entity);
    }
}