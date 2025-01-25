using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

//TO DO 
//Implement Basic Password to Join a Room 
public class PhotonManager : MonoBehaviourPunCallbacks
{
   
    #region Variables
    //input fields
    [Header("Input Fields")]
    [SerializeField] TMP_InputField userNameInputfield;//player name panel to set player name 
    [SerializeField] TMP_InputField roomNameInputfield;//create room panel to set room name 
    [SerializeField] TMP_InputField roomPasswordInputField; // UI Password input
     
    // [SerializeField] TMP_InputField maxPlayers;
    //Text
    [Header("Text")]
    [SerializeField] TMP_Text playerNameTMP;//saved player name in player prefs

    [SerializeField] TMP_Text roomInfoTMP;//Joined room panel Info for current room 
    [SerializeField] TMP_Text actionButtonTMP;//Joined room panel leave or startgame
    //GameObjects
    [Header("GameObjects")]
    [SerializeField] GameObject roomListPrefab;//Prefab for instantiating 
    [SerializeField] GameObject roomListParent;//Scroll view content 
    [SerializeField] GameObject roomCreateFailed;// same room name exists 
    #endregion
    #region Panels
    [Header("Panels")]
    [SerializeField] GameObject PlayerNamePanel;
    [SerializeField] GameObject LobbyPanel;
    [SerializeField] GameObject RoomCreatePanel;
    [SerializeField] GameObject ConnectingPanel;
    [SerializeField] GameObject RoomListPanel;
    [SerializeField] GameObject JoinedRoomPanel;

    [Header("Buttons")]
    [SerializeField] Button actionButton;        //Joined room panel leave or startgame

    #endregion
    Dictionary<string, RoomInfo> roomDataDictionary;
    Dictionary<string, GameObject> roomDataGameObjectDic;

    #region UnityMethods

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        roomDataDictionary = new Dictionary<string, RoomInfo>();
        roomDataGameObjectDic = new Dictionary<string, GameObject>();
        GetSavedPlayerData();
    }

    private void GetSavedPlayerData()
    {
        
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            string prefsSavedName = PlayerPrefs.GetString("PlayerName");
            PhotonNetwork.LocalPlayer.NickName = prefsSavedName;
            PhotonNetwork.ConnectUsingSettings();
            //PlayerNamePanel.SetActive(false);
            ActivatePanel(ConnectingPanel.name);
            playerNameTMP.text = prefsSavedName;
        }
        else
        {
            ActivatePanel(PlayerNamePanel.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            // Player is in a room
            //Debug.Log("Room Name: " + PhotonNetwork.CurrentRoom.Name +
            //          " | Player Count: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }
        else
        {
            // Player is not in a room
            //Debug.Log("Not in a room. Current network state: " + PhotonNetwork.NetworkClientState);
        }
    }
    #endregion

    #region UIMethods
    public void OnloginClick()
    {
        string name = userNameInputfield.text;
        if(!string.IsNullOrEmpty(name))
        {
            PlayerPrefs.SetString("PlayerName", name);
            PlayerPrefs.Save();
            PhotonNetwork.LocalPlayer.NickName = name;
            PhotonNetwork.ConnectToRegion("asia");
            PhotonNetwork.ConnectUsingSettings();
           // PlayerNamePanel.SetActive(false);
            ActivatePanel(ConnectingPanel.name);
        }
        else
        {
            Debug.Log("empty Login info");
        }

    }

    public void OnCreateRoomUI()
    {
        ActivatePanel(RoomCreatePanel.name);
    }
    public void OnCancelClicked()
    {
        ActivatePanel(LobbyPanel.name);
    }
    public void OnShowRooms()
    {
        if (!PhotonNetwork.InLobby)
        {
            RoomListPanel.SetActive(true);
            PhotonNetwork.JoinLobby();
        }
    }
    public void OnClickRoomCreate()
    {
        string roomNameText = roomNameInputfield.text;
        //string roomPassword = roomPasswordInputField.text;
        if (string.IsNullOrEmpty(roomNameText))
        {
            roomNameText += Random.Range(0, 10000);
        }
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        PhotonNetwork.CreateRoom(roomNameText,roomOptions);
    }
    #endregion

    #region Photon_Callbacks
    public override void OnLeftRoom()
    {
        PhotonNetwork.JoinLobby();
        ActivatePanel(LobbyPanel.name);
        RoomListPanel.SetActive(false);
    }
    public override void OnConnected()
    {
        Debug.Log("Connected internet");
        ActivatePanel(LobbyPanel.name);
        //base.OnConnected();
    }
    public override void OnConnectedToMaster() 
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName+" is connected to photon...");
        //base.OnConnectedToMaster();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomInfo();
        base.OnPlayerEnteredRoom(newPlayer);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomInfo();
        base.OnPlayerLeftRoom(otherPlayer);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning(message);
        roomCreateFailed.SetActive(true);
    }
    public override void OnLeftLobby()
    {
        ClearRoomList();//deletes every prefab created to join room
        roomDataDictionary.Clear();//clear everything inside the dictionary
        base.OnLeftLobby();
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //remove rooms created in previous updates
        ClearRoomList();


        foreach (RoomInfo roomInfo in roomList)
        {
            Debug.Log("Room name: " + roomInfo.Name);
            if (!roomInfo.IsVisible || !roomInfo.IsOpen||roomInfo.RemovedFromList)//removing list if its notvisible notOpen RemovedFromList
            {
                if (roomDataDictionary.ContainsKey(roomInfo.Name))
                {
                    roomDataDictionary.Remove(roomInfo.Name);
                }
            } 
            else if(roomDataDictionary.ContainsKey(roomInfo.Name))
            {
                //Update List
                roomDataDictionary[roomInfo.Name] = roomInfo;
            }
            else 
            { 
                //Or Add 
                roomDataDictionary.Add(roomInfo.Name,roomInfo);
            }
        }

        //create join room gameoobject
        foreach (RoomInfo roomInfo in roomDataDictionary.Values)
        {
            GameObject roomListGameobject = Instantiate(roomListPrefab);
            roomListGameobject.transform.SetParent(roomListParent.transform);
            roomListGameobject.transform.localScale = Vector3.one;

            roomListGameobject.transform.GetChild(0).GetComponent<TMP_Text>().text = roomInfo.Name + "       " +roomInfo.PlayerCount + " | "+ roomInfo.MaxPlayers;
            roomListGameobject.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(()=>JoinFromList(roomInfo.Name));
            roomDataGameObjectDic.Add(roomInfo.Name,roomListGameobject);
        }
    }
    private void JoinFromList(string roomName)
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    private void ClearRoomList()
    {
        if (roomDataGameObjectDic.Count > 0)
        {
            foreach (GameObject v in roomDataGameObjectDic.Values)
            {
                Destroy(v);
            }
            roomDataGameObjectDic.Clear();
        }
    }

    public override void OnCreatedRoom()
    {

        Debug.Log(PhotonNetwork.CurrentRoom + "is Created!");
        //base.OnCreatedRoom();
    }
    public override void OnJoinedRoom()
    {

        Debug.Log(PhotonNetwork.LocalPlayer.NickName + "has joined a room");
        JoinRoom();
        //base.OnJoinedRoom();
    }
    void JoinRoom()
    {
        UpdateRoomInfo();
        if (PhotonNetwork.IsMasterClient)
        {
            actionButtonTMP.text = "Start Game";
            actionButton.onClick.AddListener(StartGame);
        }
        else
        {
            actionButtonTMP.text = "Leave Lobby";
            actionButton.onClick.AddListener(LeaveRoomManually);
        }
    }

    private void UpdateRoomInfo()
    {
        ActivatePanel(JoinedRoomPanel.name);
        roomInfoTMP.text = "Host RoomName : " + PhotonNetwork.CurrentRoom.Name +
                "\n Players: " + PhotonNetwork.CurrentRoom.PlayerCount + " | " + PhotonNetwork.CurrentRoom.MaxPlayers;
        int i = 1;
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            roomInfoTMP.text += "\n Player"+i+":-"+player.NickName;
            i++;
        }
    }

    void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");//change game scene in every client
        }
    }
    void LeaveRoomManually()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == ClientState.Joined)
        { 
            PhotonNetwork.LeaveRoom();        // Leave the current room
            ActivatePanel(LobbyPanel.name);  // Go back to the lobby panel
            RoomListPanel.SetActive(false); // Close the room list panel
        }
    }

    #endregion


    #region Public_Methods

    public void ActivatePanel(string panelName)
    {
        LobbyPanel.SetActive(panelName.Equals(LobbyPanel.name));
        PlayerNamePanel.SetActive(panelName.Equals(PlayerNamePanel.name));
        RoomCreatePanel.SetActive(panelName.Equals(RoomCreatePanel.name));
        ConnectingPanel.SetActive(panelName.Equals(ConnectingPanel.name));
        JoinedRoomPanel.SetActive(panelName.Equals(JoinedRoomPanel.name));  
    }
    #endregion


}
