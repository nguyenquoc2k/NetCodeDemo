using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Character : NetworkBehaviour
{
    public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(5f); // Giá trị mặc định
    protected Rigidbody2D rb;
    private float horizontal;
    private float vertical;
    private Vector3 movement;
    public override void OnNetworkSpawn()
    {
        moveSpeed.OnValueChanged += HandleSpeed;
    }

    private void HandleSpeed(float previousvalue, float newvalue)
    {
        if (!IsServer)
        {
            moveSpeed.Value = newvalue;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        this.WaitEndFrame((() =>
        {
            UIManager.Instance.HandleClientWhenConnected();
        }));
            
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetMoveSpeedServerRpc(float newSpeed, ulong clientId)
    {
        // Đảm bảo logic chỉ áp dụng cho client được chỉ định
        if(newSpeed<2) newSpeed = 2.5f;
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject != null)
        {
            var player = playerObject.GetComponent<Player>();
            if (player != null)
            {
                player.moveSpeed.Value = newSpeed;
            }
        }
    }


    
    protected virtual void HandleMovement()
    {
        horizontal = Input.GetAxis("Horizontal"); 
        vertical  = Input.GetAxis("Vertical"); 
        movement = new Vector2(horizontal,vertical ).normalized;
        rb.velocity = movement * moveSpeed.Value;
    }

    private void Update()
    {
        if(!IsOwner) return;
        HandleMovement();
    }
}