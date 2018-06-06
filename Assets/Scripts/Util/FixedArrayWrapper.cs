using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System;

[RequireComponent(typeof(GameObjectEntity))]
public class FixedArrayWrapper<T> : MonoBehaviour, ISerializationCallbackReceiver
    where T : struct
{
    [SerializeField]
    public T[] array;

    int length;

    void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        if (gameObjectEntity == null)
            return;
        if (gameObjectEntity.EntityManager == null)
            return;
        if (!gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
            return;

        length = array.Length;
        gameObjectEntity.EntityManager.AddComponent(gameObjectEntity.Entity, GetComponentType());
    }


    void OnValidate()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        if (array.Length == 0)
            return;
        if (gameObjectEntity == null)
            return;
        if (gameObjectEntity.EntityManager == null)
            return;
        if (!gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
            return;
        if (!gameObjectEntity.EntityManager.HasComponent(gameObjectEntity.Entity, GetComponentType()))
            return;

        UpdateComponentData(gameObjectEntity.EntityManager, gameObjectEntity.Entity);
    }

    public void OnBeforeSerialize()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        if (array.Length == 0)
            return;
        if (gameObjectEntity == null)
            return;
        if (gameObjectEntity.EntityManager == null)
            return;
        if (!gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
            return;
        if (!gameObjectEntity.EntityManager.HasComponent(gameObjectEntity.Entity, GetComponentType()))
            return;
        UpdateSerializedData(gameObjectEntity.EntityManager, gameObjectEntity.Entity);
    }

    public void OnAfterDeserialize() { }


    ComponentType GetComponentType()
    {
        return ComponentType.FixedArray(typeof(T), length);
    }

    void UpdateComponentData(EntityManager manager, Entity entity)
    {
        var c = manager.GetFixedArray<T>(entity);
        if (array.Length != c.Length)
        {
            manager.RemoveComponent(entity, ComponentType.FixedArray(typeof(T), c.Length));
            length = array.Length;
            manager.AddComponent(entity, GetComponentType());
            c = manager.GetFixedArray<T>(entity);
        }
        c.CopyFrom(array);


    }
    void UpdateSerializedData(EntityManager manager, Entity entity)
    {
        var c = manager.GetFixedArray<T>(entity);
        Array.Resize(ref array, c.Length);
        c.CopyTo(array);
    }
}