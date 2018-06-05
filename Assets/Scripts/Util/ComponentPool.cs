using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ComponentPool<T> 
    where T : Component
{
    List<T> objects = new List<T>();
    public GameObject prefab;
    int archor = 0;

    public T New()
    {
        if(archor < objects.Count)
        {
            objects[archor].gameObject.SetActive(true);
            return objects[archor++];
        }
        else
        {
            if(prefab == null)
            {
                var newGO = new GameObject("PooledObject");
                objects.Add(newGO.AddComponent<T>());
                return objects[archor++];
            }
            else
            {
                var newGO = Object.Instantiate(prefab);
                objects.Add(newGO.GetComponent<T>());
                return objects[archor++];
            }
        }
    }

    public void Present()
    {
        for(var i=archor;i<objects.Count;++i)
        {
            objects[i].gameObject.SetActive(false);
        }
        archor = 0;
    }
}
