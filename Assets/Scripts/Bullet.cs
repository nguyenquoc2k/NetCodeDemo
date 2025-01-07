using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.transform.GetChild(0).GetComponent<SpriteRenderer>().color == Color.white)
        {
            transform.localScale = Vector3.zero;

            var player = other.GetComponent<Player>();
            if (player != null)
            {
                // Gửi yêu cầu thay đổi tốc độ di chuyển lên server
                player.SetMoveSpeedServerRpc(player.moveSpeed.Value / 2, player.OwnerClientId);

                // Khôi phục tốc độ sau 5 giây
                this.SetTimeout(() => player.SetMoveSpeedServerRpc(5f, player.OwnerClientId), 5f);
            }
        }
    }


  
}
