﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

internal static class SchemaUtils
{
    internal static ComponentSchema RegisterComponentTypes(TypeStore typeStore)
    {
        var assemblyLoader  = new AssemblyLoader();
        var assemblies      = assemblyLoader.GetEngineDependants();
        
        var dependants  = assemblyLoader.dependants;
        foreach (var assembly in assemblies) {
            var types           = AssemblyLoader.GetComponentTypes(assembly);
            var componentTypes  = new List<SchemaType>();
            foreach (var type in types) {
                var componentType = CreateComponentType(type, typeStore);
                componentTypes.Add(componentType);
            }
            dependants.Add(new EngineDependant (assembly, componentTypes));
        }
        Console.WriteLine(assemblyLoader);
        
        var structs     = new List<SchemaType>();
        var scripts     = new List<SchemaType>();
        var tags        = new List<SchemaType>();
        foreach (var dependant in dependants)
        {
            foreach (var type in dependant.Types)
            {
                switch (type.kind) {
                    case ComponentKind.Script:      scripts.Add(type);  break;
                    case ComponentKind.Component:   structs.Add(type);  break;
                    case ComponentKind.Tag:         tags.Add(type);     break;
                }
            }
        }
        return new ComponentSchema(dependants, structs, scripts, tags);
    }
    
    internal static SchemaType CreateComponentType(Type type, TypeStore typeStore)
    {
        const BindingFlags flags    = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        
        if (type.IsValueType && typeof(IEntityTag).IsAssignableFrom(type)) {
            var method          = typeof(SchemaUtils).GetMethod(nameof(CreateTagType), flags);
            var genericMethod   = method!.MakeGenericMethod(type);
            var componentType   = (SchemaType)genericMethod.Invoke(null, null);
            return componentType;
        }
        var createParams = new object[] { typeStore };
        foreach (var attr in type.CustomAttributes)
        {
            var attributeType = attr.AttributeType;
            if (attributeType == typeof(ComponentAttribute))
            {
                var method          = typeof(SchemaUtils).GetMethod(nameof(CreateStructFactory), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var componentType   = (SchemaType)genericMethod.Invoke(null, createParams);
                return componentType;
            }
            if (attributeType == typeof(ScriptAttribute))
            {
                var method          = typeof(SchemaUtils).GetMethod(nameof(CreateClassFactory), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var componentType   = (SchemaType)genericMethod.Invoke(null, createParams);
                return componentType;
            }
        }
        throw new InvalidOperationException($"missing expected attribute. Type: {type}");
    }
    
    internal static SchemaType CreateStructFactory<T>(TypeStore typeStore)
        where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        var structKey   = StructHeap<T>.StructKey;
        return new StructComponentType<T>(structKey, structIndex, typeStore);
    }
    
    internal static SchemaType CreateClassFactory<T>(TypeStore typeStore)
        where T : Script
    {
        var scriptIndex   = ClassType<T>.ScriptIndex;
        var scriptKey     = ClassType<T>.ScriptKey;
        return new ScriptType<T>(scriptKey, scriptIndex, typeStore);
    }
    
    internal static SchemaType CreateTagType<T>()
        where T : struct, IEntityTag
    {
        var tagIndex    = TagType<T>.TagIndex;
        return new TagType(typeof(T), tagIndex);
    }
}