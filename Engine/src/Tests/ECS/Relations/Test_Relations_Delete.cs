﻿using System.Linq;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.Relations {


public static class Test_Relations_Delete
{
    [Test]
    public static void Test_Relations_Delete_Int()
    {
        var store       = new EntityStore();
        var entities    = store.GetAllEntitiesWithRelations<IntRelation>();
        AreEqual("{ }",         entities.ToStr());

        var entity1 = store.CreateEntity(1);
        var entity2 = store.CreateEntity(2);
        
        entity1.AddComponent(new IntRelation { value = 10 });
        entity1.AddComponent(new IntRelation { value = 20 });
        AreEqual("{ 1 }",       entities.ToStr());
        
        entity2.AddComponent(new IntRelation { value = 30 });
        AreEqual("{ 1, 2 }",    entities.ToStr());
        
        int count = 0;
        store.ForAllEntityRelations((ref IntRelation relation, Entity entity) => {
            switch (count++) {
                case 0: AreEqual(1, entity.Id); AreEqual(10, relation.value); break;
                case 1: AreEqual(1, entity.Id); AreEqual(20, relation.value); break;
                case 2: AreEqual(2, entity.Id); AreEqual(30, relation.value); break;
            }
        });
        AreEqual(3, count);
        
        entity1.DeleteEntity();
        AreEqual("{ 2 }",       entities.ToStr());
        var array = entities.ToArray();
        AreEqual(30, array[0].GetRelation<IntRelation, int>(30).value);
        
        entity2.DeleteEntity();
        AreEqual("{ }",         entities.ToStr());
    }
}

}
