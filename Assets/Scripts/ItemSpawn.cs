using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawn : NetworkBehaviour
{
    public UIManager itemSpawner;
    public NetworkVariable<bool> isTriggerItem = new NetworkVariable<bool>(); 
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isTriggerItem.OnValueChanged +=OnClientTriggerItem;
        }
      
    }
    private void OnClientTriggerItem(bool previousvalue, bool newvalue)
    {
       // if (IsServer&& newvalue)
          //  StartCoroutine(UIManager.Instance.HandleSpawnItem(true));
    }

    

   
}
