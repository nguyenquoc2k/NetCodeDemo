using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class CharacterSetting : NetworkBehaviour
{
    [SerializeField] private TextMeshPro characterName;

    NetworkVariable<NetWorkString> networkCharacterName = new NetworkVariable<NetWorkString>("Unknow",
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> clientCount = new NetworkVariable<int>(0);
    public NetworkVariable<Color> color = new NetworkVariable<Color>(Color.white);
    public NetworkVariable<Color> colorTrigger = new NetworkVariable<Color>(Color.white);
    public NetworkVariable<int> clientId = new NetworkVariable<int>(0);
    public NetworkVariable<int> bulletAmount = new NetworkVariable<int>(0);
    int maxClientCount;
    public Color hostColor;
    public GameObject bulletPrefab; // Prefab của đạn
    float bulletSpeed = 20f; // Tốc độ của đạn
    private float timer = 0f;
    private bool isRed = false;

    private void Awake()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RandomChangeColorServerRpc(int clientCount)
    {
        this.WaitUntil(() =>
        {
            if (!IsServer)
            {
                return;
            }

            if (this.clientCount.Value < clientCount) return;
            if (clientCount <= maxClientCount) return;
            maxClientCount = clientCount;

            var clients = NetworkManager.Singleton.ConnectedClientsList;

            int randomIndex = Random.Range(0, clients.Count);
            ulong selectedClientId = clients[randomIndex].ClientId;
            Color newColor = Color.red;
            color.Value = newColor;
            UIManager.Instance.characterSettings[randomIndex].colorTrigger.Value = newColor;
            clientId.Value = randomIndex + 1;
            UIManager.Instance.characterSettings[randomIndex].transform.GetChild(0).GetComponent<SpriteRenderer>()
                .color = newColor;
            UIManager.Instance.isEnoughPlayer.Value = true;
        }, () => UIManager.Instance.characterSettings.Count > 0);

    }

    private void OnClientChange(int newValue)
    {
        if (clientId.Value == newValue)
        {
            UIManager.Instance.characterSettings[newValue - 1].transform.GetChild(0).GetComponent<SpriteRenderer>()
                .color = hostColor;
        }
    }

    private void OnColorChange(Color newValue)
    {
        hostColor = newValue;
    }


    private void HandleClientConnected(ulong obj)
    {
        if (IsServer)
            clientCount.Value++;
        Debug.Log($"Client {obj} đã tham gia vào server.");

    }


    private void HandleClientDisconnect(ulong obj)
    {
        if (IsServer)
        {
            clientId.Value = 100;
            clientCount.Value--;
            color.Value = Color.gray;
        }

        Debug.Log($"Client {obj} đã thoát server.");
        if (this == null) return;
        this.WaitEndFrame((() =>
        {
            maxClientCount = 0;
            for (int i = UIManager.Instance.characterSettings.Count - 1; i >= 0; i--)
            {
                if (UIManager.Instance.characterSettings[i] == null)
                {
                    Debug.Log($"Removing missing child at index {i}");
                    UIManager.Instance.characterSettings.RemoveAt(i);
                }
                else
                {
                    UIManager.Instance.characterSettings[i].transform.GetChild(0).GetComponent<SpriteRenderer>().color =
                        Color.white;
                }
            }
        }));
    }

    private void CheckClientCount(int newClientCount)
    {
        if (this != null)
            this.WaitEndFrame((() => { UIManager.Instance.HandleClientWhenConnected(); }));
    }

    // Override OnNetworkSpawn để đảm bảo đồng bộ hóa clientCount khi client tham gia
    public override void OnNetworkSpawn()
    {

        if (IsOwner && !string.IsNullOrEmpty(UIManager.Instance.nameInput.text))
        {
            networkCharacterName.Value = UIManager.Instance.nameInput.text;
        }

        characterName.text = networkCharacterName.Value.ToString();
        networkCharacterName.OnValueChanged += NetworkCharacterName_OnChangeValued;
        if (!IsServer)
        {
            clientCount.OnValueChanged += (oldValue, newValue) => CheckClientCount(newValue);
            color.OnValueChanged += (oldValue, newValue) => OnColorChange(newValue);
            clientId.OnValueChanged += (oldValue, newValue) => OnClientChange(newValue);
            // Đăng ký sự kiện thay đổi màu
            colorTrigger.OnValueChanged += OnClientChangeColorTrigger;
            bulletAmount.OnValueChanged += HandleBulletAmount;
        }

    }

    private void HandleBulletAmount(int previousvalue, int newvalue)
    {
        if (!IsServer)
        {
            bulletAmount.Value = newvalue;
        }
    }

    private Vector3 mousePosition;

    void SpawnBullet()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        ReduceBulletServerRpc(mousePosition);
    }

    [ServerRpc]
    void ReduceBulletServerRpc(Vector3 mousePosition)
    {

        Debug.Log(mousePosition);
        mousePosition.z = 0;
        Vector3 direction = (mousePosition - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * bulletSpeed;
        }

        if (bulletAmount.Value > 0)
        {
            bulletAmount.Value--;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && transform.GetChild(0).GetComponent<SpriteRenderer>().color == Color.red)
        {
            if (!IsOwner) return; // Chỉ owner mới được phép bắn đạn
            if (bulletAmount.Value > 0)
                SpawnBullet();
        }

        HanldeTimeRed();
    }

    private void HanldeTimeRed()
    {
        if (IsOwner)
        {
            SpriteRenderer spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();

            // Kiểm tra xem màu của SpriteRenderer có phải là màu đỏ không
            if (spriteRenderer.color == Color.red)
            {
                timer += Time.deltaTime;
                UIManager.Instance.timeRedTxt.text = "Time It: " + timer.ToString("00");

            }
        }
    }

private bool isProcessedTrigger;
 
   private void OnTriggerEnter2D(Collider2D other)
   {
       if (other.transform.GetInstanceID() > transform.GetInstanceID()) return;
       if (other.CompareTag("Player") && !isProcessedTrigger&& IsServer)
       {
           SpriteRenderer playerSprite = other.transform.GetChild(0).GetComponent<SpriteRenderer>();
           SpriteRenderer triggerSprite = transform.GetChild(0).GetComponent<SpriteRenderer>();

           if (playerSprite.color == Color.white && triggerSprite.color == Color.red)
           {
              
                   playerSprite.color = Color.red;
                   triggerSprite.color = Color.white;
                   other.transform.GetComponent<CharacterSetting>().colorTrigger.Value = Color.red;
                   colorTrigger.Value = Color.white;
                   bulletAmount.Value = 0;
                   other.transform.GetComponent<CharacterSetting>().bulletAmount.Value = 0;

           }
           else if (playerSprite.color == Color.red && triggerSprite.color == Color.white)
           {
                   playerSprite.color = Color.white;
                   triggerSprite.color = Color.red;
                   other.transform.GetComponent<CharacterSetting>().colorTrigger.Value = Color.white;
                   colorTrigger.Value = Color.red;
           }

           isProcessedTrigger = true;
       }
       else if(other.CompareTag("Item"))
       {
           //Destroy(other.gameObject);
           other.transform.localScale = Vector3.zero;
           if(IsServer)
            other.transform.GetComponent<ItemSpawn>().isTriggerItem.Value = true;
           if (transform.GetChild(0).GetComponent<SpriteRenderer>().color == Color.white)
           {
               if (IsServer)
               {
                   GetComponent<Player>().moveSpeed.Value = 10;
                   this.SetTimeout(() => GetComponent<Player>().moveSpeed.Value = 5, 5f);
               }
           }
           else if (transform.GetChild(0).GetComponent<SpriteRenderer>().color == Color.red)
           {
               if(IsServer)
                bulletAmount.Value = 3;
           }
       }
   }

   private void OnTriggerExit2D(Collider2D other)
   {
       if(!IsServer) return;
       isProcessedTrigger = false;
       if(other.CompareTag("Item"))
           other.transform.GetComponent<ItemSpawn>().isTriggerItem.Value = false;
   }
   
   private void OnClientChangeColorTrigger(Color oldValue, Color newValue)
   {
       if(IsServer) return;
       // Update the sprite color on all clients when the NetworkVariable changes
       transform.GetChild(0).GetComponent<SpriteRenderer>().color = newValue;
   }

   private void NetworkCharacterName_OnChangeValued(NetWorkString previousvalue, NetWorkString newvalue)
   {
     characterName.text = newvalue.ToString(); 
   }
   
   

   public struct NetWorkString : INetworkSerializeByMemcpy
   {
       private ForceNetworkSerializeByMemcpy<FixedString32Bytes> info;

       public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
       {
           serializer.SerializeValue(ref info);
       }

       public override string ToString()
       {
           return info.Value.ToString();
       }

       public static implicit operator string(NetWorkString s) => s.ToString();
       public static implicit operator NetWorkString(string s) => new NetWorkString() { info = new FixedString32Bytes(s) };
   }
}
