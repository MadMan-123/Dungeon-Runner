using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core
{
    // Simple server-side pool for NetworkObjects.
    public class NetworkGameObjectPool : IEnumerable<NetworkObject>
    {
        private List<NetworkObject> objects;          // All pooled instances (active + inactive)
        private HashSet<NetworkObject> availableObjects; // Currently unused objects
        private NetworkObject prefab;                // Prefab this pool is for
        private Transform pTransform;                // Parent transform for organisation

        public int Count => objects.Count;
        public readonly bool Dynamic = true;         // Pool can grow if empty

        // Create a pool from a single prefab
        public NetworkGameObjectPool(NetworkObject prefab, int initialSize, Transform parent)
        {
            // Only the server should build the pool
            if (!NetworkManager.Singleton.IsServer)
                return;

            this.prefab = prefab;
            pTransform = parent;

            objects = new List<NetworkObject>(initialSize);
            availableObjects = new HashSet<NetworkObject>();

            // Pre-instantiate pooled objects
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateObjectInPool(prefab);

                obj.transform.SetParent(pTransform);
                obj.gameObject.SetActive(false);
                availableObjects.Add(obj);
            }

            Debug.Log($"Pool Created: {(prefab ? prefab.name : "Empty")}, Pool Size: {objects.Count}");
        }

        // Just instantiates and registers an object in the pool (doesn't spawn it)
        private NetworkObject CreateObjectInPool(NetworkObject prefabToUse)
        {
            var obj = Object.Instantiate(prefabToUse, Vector3.zero, Quaternion.identity, pTransform);
            obj.gameObject.SetActive(false);
            obj.tag = "Pooled";

            objects.Add(obj);
            return obj;
        }

        // Fetch an object from the pool
        public NetworkObject GetObject()
        {
            // Grab an available object if we have one
            if (availableObjects.Count > 0)
            {
                var enumerator = availableObjects.GetEnumerator();
                enumerator.MoveNext();
                var obj = enumerator.Current;

                availableObjects.Remove(obj);
                obj.gameObject.SetActive(true);

                // Server would call Spawn here normally, but code has it disabled

                return obj;
            }

            // Pool empty so create a new one if dynamic
            if (Dynamic && prefab != null && NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("Pool exhausted, creating new pooled object");

                var newObj = CreateObjectInPool(prefab);
                newObj.gameObject.SetActive(true);
                newObj.transform.SetParent(pTransform);

                return newObj;
            }

            // No prefab or dynamic disabled
            return null;
        }

        // Return object back to the pool
        public void ReturnObject(NetworkObject obj)
        {
            if (obj == null || availableObjects.Contains(obj))
                return; 

            if (obj.IsSpawned)
                obj.Despawn(false); // Don't destroy it, just unspawn

            obj.gameObject.SetActive(false);
            availableObjects.Add(obj);
        }

        public List<NetworkObject> GetAllObjects() => objects;

        public bool Contains(NetworkObject obj) => objects.Contains(obj);

        // Clears the pool completely (mostly for debugging or teardown)
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
