using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(NetworkObject))]
public class Weapon : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private NetworkObject bulletPrefab;

    private Camera cache;

    public override void OnNetworkSpawn()
    {
        cache = Camera.main;
        Debug.Log($"Weapon spawned. IsServer: {IsServer}, PoolManager exists: {PoolManager.Instance != null}");
        base.OnNetworkSpawn();
    }

    

    private void Update()
    {
        if (!IsOwner) return;
        
        Vector3 mousePos = cache.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // ensure 2D plane
        Vector3 direction = (mousePos - transform.position).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootServerRPC(transform.position + direction, direction);
        }
    }

    [ServerRpc]
    private void ShootServerRPC(Vector3 pos, Vector3 dir)
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogError("PoolManager.Instance is null");
            return;
        }
    
        var pool = PoolManager.Instance.GetPool("Bullets");
        if (pool == null)
        {
            Debug.LogError("Bullet pool not found!");
            return;
        }
    
        var bullet = pool.GetObject();
        if (bullet == null) return;

        if (bullet.TryGetComponent(out Projectile proj))
        {
            
            //proj.Init(pos, dir * speed, pool);
        }
    }
}