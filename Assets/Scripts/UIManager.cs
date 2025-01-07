using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIManager : NetworkBehaviour 
{
    public Button hostButton, joinButton;
    public TMP_InputField nameInput;
    public static UIManager Instance;
    public TextMeshProUGUI noticeText,countDownText,timeRedTxt;
    public List<CharacterSetting> characterSettings = new List<CharacterSetting>();
    [SerializeField] private Transform enemyPrefab;
    [SerializeField] private Vector2 minSpawnPos;
    [SerializeField] private Vector2 maxSpawnPos;
    [SerializeField] private int maxItemCount = 100;
    public NetworkVariable<bool> isEnoughPlayer = new NetworkVariable<bool>(); 
    private float countdownTime = 120f;  
    private NetworkVariable<float> networkCountdownValue = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); });
        joinButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); });
      
        isEnoughPlayer.OnValueChanged += HandleItem;
    }

    private void Update()
    {
        if (IsClient)
        {
            string timeFormatted = FormatTime(networkCountdownValue.Value);
            countDownText.text = timeFormatted;
        }
    }
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);  // Lấy phần phút
        int seconds = Mathf.FloorToInt(time % 60);  // Lấy phần giây

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    private void HandleItem(bool previousvalue, bool newvalue)
    {
        if (IsServer)
        {
            StartCoroutine(HandleSpawnItem(newvalue));
            StartCountdown();
        }
            
    }
    private void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }

    // Coroutine để thực hiện countdown
    private IEnumerator CountdownCoroutine()
    {
        while (countdownTime > 0f)
        {
            countdownTime -= Time.deltaTime;
            networkCountdownValue.Value = countdownTime;  // Cập nhật giá trị countdown cho tất cả client
            yield return null;
        }

        // Khi countdown kết thúc, thực hiện hành động nào đó
        OnCountdownComplete();
    }

    // Xử lý khi countdown kết thúc
    private void OnCountdownComplete()
    {
        // Thực hiện hành động khi countdown kết thúc
        Debug.Log("Countdown finished!");
    }

    public void HandleClientWhenConnected()
    {
       

        // Tìm tất cả GameObjects có tên là "Player(Clone)"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        GameObject playerWithMaxClientCount = null;
        int maxClientCount = -1;

        // Duyệt qua tất cả các gameobject và kiểm tra IsHost
        foreach (GameObject player in players)
        {
            if (player.name == "Player(Clone)")
            {
                var characterSetting = player.GetComponent<CharacterSetting>();
            
                // Kiểm tra nếu `characterSetting` chưa tồn tại trong danh sách
                if (!characterSettings.Contains(characterSetting))
                {
                    characterSettings.Add(characterSetting);
                }
                // Lấy clientCount từ CharacterSetting
                int currentClientCount = player.GetComponent<CharacterSetting>().clientCount.Value;

                // Kiểm tra nếu currentClientCount lớn hơn maxClientCount hiện tại
                if (currentClientCount > maxClientCount)
                {
                    maxClientCount = currentClientCount;
                    playerWithMaxClientCount = player; // Cập nhật player có clientCount lớn nhất
                }
            }
        }

        // Nếu tìm được player có clientCount lớn nhất, gọi CheckClientCount
        if (playerWithMaxClientCount != null)
        {
            if (playerWithMaxClientCount.GetComponent<CharacterSetting>().clientCount.Value == 4 )
            {           
             //   noticeText.text = "Start Random Ghost";
                characterSettings.ForEach(x=>x.GetComponent<CharacterSetting>().RandomChangeColorServerRpc(4));
                if(IsServer)
                    isEnoughPlayer.Value = true;
            }
            else
                noticeText.text = "";
        }
    }
    public List<Transform> items = new List<Transform>();
    
    public IEnumerator HandleSpawnItem(bool isEnoughPlayer)
    {
        float lastSpawnTime = 0f; // Biến lưu trữ thời gian lần spawn trước

        while (true)
        {
            int count = 0;
            foreach (var item in items)
            {
                if (item.localScale == Vector3.one)
                {
                    count++;
                }
            }

            if (count < maxItemCount && isEnoughPlayer)
            {
                // Kiểm tra xem có đủ thời gian để spawn hay không
                if (Time.time - lastSpawnTime >= 15f)
                {
                    Vector2 spawnPos = new Vector2(Random.Range(minSpawnPos.x, maxSpawnPos.x), Random.Range(minSpawnPos.y, maxSpawnPos.y));
                    Transform enemyTransform = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                    enemyTransform.GetComponent<ItemSpawn>().itemSpawner = this;
                    enemyTransform.GetComponent<NetworkObject>().Spawn();
                    items.Add(enemyTransform);

                    lastSpawnTime = Time.time; // Cập nhật thời gian spawn lần này

                    yield return new WaitForSeconds(15);
                }
            }

            yield return null;
        }
    }

}
