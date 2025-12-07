using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core
{
    public class NetworkGameObjectPool : IEnumerable<NetworkObject>
    {
        private List<NetworkObject> objects;
        private HashSet<NetworkObject> availableObjects;
        private NetworkObject prefab;
        private Transform pTransform;

        public int Count => objects.Count;
        public readonly bool Dynamic = true;

        // Constructor for single prefab
        public NetworkGameObjectPool(NetworkObject prefab, int initialSize, Transform parent)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            this.prefab = prefab;
            pTransform = parent;
            objects = new List<NetworkObject>(initialSize);
            availableObjects = new HashSet<NetworkObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateObjectInPool(prefab);
                obj.Spawn(true); // Spawn immediately so OnNetworkSpawn fires

                obj.transform.SetParent(pTransform); 
                obj.gameObject.SetActive(false); // But keep inactive 
                availableObjects.Add(obj);
            }

            Debug.Log($"Pool Created: {(prefab ? prefab.name : "Empty Object")}, Pool Size: {objects.Count}");
        }

        // Create object but do NOT spawn yet
        private NetworkObject CreateObjectInPool(NetworkObject prefabToUse)
        {
            var obj = Object.Instantiate(prefabToUse, Vector3.zero, Quaternion.identity, pTransform);
            obj.gameObject.SetActive(false);
            obj.tag = "Pooled";
    
            objects.Add(obj);
            return obj;
        }

        public NetworkObject GetObject()
        {
            if (availableObjects.Count > 0)
            {
                var enumerator = availableObjects.GetEnumerator();
                enumerator.MoveNext();
                var obj = enumerator.Current;
                availableObjects.Remove(obj);

                obj.transform.SetParent(pTransform); // Ensure still parented
                obj.gameObject.SetActive(true);
                return obj;
            }

            if (Dynamic && prefab != null && NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("Pool exhausted! Creating dynamic object");
                var newObj = CreateObjectInPool(prefab);
                newObj.Spawn(true);
                newObj.transform.SetParent(pTransform); // Parent the dynamic one too
                newObj.gameObject.SetActive(true);
                return newObj;
            }

            return null;
        }

        // Return object to pool safely
        public void ReturnObject(NetworkObject obj)
        {
            if (obj == null || availableObjects.Contains(obj))
                return;

            if (obj.IsSpawned)
                obj.Despawn();

            obj.gameObject.SetActive(false);
            availableObjects.Add(obj);
        }

        public List<NetworkObject> GetAllObjects() => objects;

        public bool Contains(NetworkObject obj) => objects.Contains(obj);

        public void CleanUp()
        {
            objects.Clear();
            availableObjects.Clear();
            GC.Collect();
        }

        public IEnumerator<NetworkObject> GetEnumerator() => objects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}