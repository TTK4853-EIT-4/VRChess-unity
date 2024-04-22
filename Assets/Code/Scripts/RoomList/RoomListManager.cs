using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomListManager : MonoBehaviour
{

    private Transform roomListContainer;
    private Transform roomRowTemplate;
    
    // Create Room Panel
    public GameObject createRoomPanel;

    // Team color choose dropdown
    public TMPro.TMP_Dropdown teamColorDropdown;

    private List<UIRoom> uIRooms = new List<UIRoom>();

    // Status Message TMPro
    public TMPro.TextMeshProUGUI createRoomStatusMessageText;
    private Color errorColor = new Color32(255, 114, 144, 255);
    private Color successColor = new Color32(147, 255, 114, 255);

    // On Awake
    private void Awake()
    {
        try
        {
            roomListContainer = GameObject.Find("roomListContainer").transform;
            roomRowTemplate = roomListContainer.Find("roomRowTemplate");
            createRoomPanel = GameObject.Find("Create Room Panel");

            // Hide the create room panel
            createRoomPanel.SetActive(false);

            // Hide the template
            roomRowTemplate.gameObject.SetActive(false);

            UnityThread.executeInUpdate(() =>
            {
                createRoomStatusMessageText.enabled = false;
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        // On room_updated event
        SocketManager.Instance.socket.On("room_updated", response =>
        {
            Room room = JsonConvert.DeserializeObject<Room>(response.GetValue<string>(0));
            UpdateRoom(room);
        });
    }

    // Start is called before the first frame update
    void Start()
    {

        // Check if the socket is connected
        if (SocketManager.Instance.socket.Connected)
        {
            GetAllRooms();
        }
        else
        {
            // Wait for the socket to connect
            SocketManager.Instance.socket.OnConnected += (sender, e) =>
            {
                //Debug.Log("socket.OnConnected");
                GetAllRooms();
            };
        }

        // On room_created event
        SocketManager.Instance.socket.On("room_created", response =>
        {
            Room room = JsonConvert.DeserializeObject<Room>(response.GetValue<string>(0));
            AddRoom(room);
        });

        // On room_deleted event
        SocketManager.Instance.socket.On("room_deleted", response =>
        {
            RoomDeletedResponse resp = response.GetValue<RoomDeletedResponse>(0);
            UIRoom room = uIRooms.Find(x => x.room.roomId == resp.roomId);

            if (room != null)
            {
                UnityThread.executeInUpdate(() =>
                {
                    Destroy(room.gameObject);
                    uIRooms.Remove(room);
                });
            }
        });
    }

    // Get all rooms
    public void GetAllRooms()
    {
        SocketManager.Instance.socket.Emit("get_all_rooms", response =>
        {
            try
            {
                string respons = response.GetValue<string>(0);
                List<Room> items = JsonConvert.DeserializeObject<List<Room>>(respons);

                foreach (var room in items)
                {
                    AddRoom(room);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        });
    }

    // Add room in the UI list
    public void AddRoom(Room room)
    {
        UnityThread.executeInUpdate(() =>
        {
            Transform roomRowTransform = Instantiate(roomRowTemplate, roomListContainer);

            roomRowTransform.Find("roomId").GetComponent<TMPro.TextMeshProUGUI>().text = room.roomId;
            roomRowTransform.Find("roomHost").GetComponent<TMPro.TextMeshProUGUI>().text = room.roomOwner.username;
            if (room.roomOpponent != null)
            {
                roomRowTransform.Find("roomOpponent").GetComponent<TMPro.TextMeshProUGUI>().text = room.roomOpponent.username;
            }
            else
            {
                roomRowTransform.Find("roomOpponent").GetComponent<TMPro.TextMeshProUGUI>().text = "";
            }

            roomRowTransform.Find("roomStatus").GetComponent<TMPro.TextMeshProUGUI>().text = room.gameStatus.ToString();

            // Count number of players
            int players = 1;
            if (room.roomOpponent != null)
            {
                players = 2;
            }
            roomRowTransform.Find("roomPlayers").GetComponent<TMPro.TextMeshProUGUI>().text = players.ToString() + "/2";
            roomRowTransform.Find("roomObservers").GetComponent<TMPro.TextMeshProUGUI>().text = room.observers.Count.ToString();

            // dropdownHolder element
            Transform dropdownHolder = roomRowTransform.Find("dropdownHolder");

            // dropdown
            Transform dropdown = dropdownHolder.Find("dropdown");
            TMPro.TMP_Dropdown dropdownComponent = dropdown.GetComponent<TMPro.TMP_Dropdown>();

            // Clear the options
            dropdownComponent.ClearOptions();

            // Add - Select an option - option
            dropdownComponent.options.Add(new TMPro.TMP_Dropdown.OptionData("Select an option"));

            if(UserData.Instance.loggedUser != null && UserData.Instance.loggedUser.id == room.roomOwner.id)
            {
                // Add - Delete - option
                dropdownComponent.options.Add(new TMPro.TMP_Dropdown.OptionData("Delete"));

                // Set background color to #688A98 on roomRowTransform element to indicate that the user is the owner of the room
                roomRowTransform.GetComponent<UnityEngine.UI.Image>().color = new Color32(104, 138, 152, 170);

                // Add on the top of the roomListContainer element
                roomRowTransform.SetAsFirstSibling();
            }
            else
            {
                // Add - Join - option
                dropdownComponent.options.Add(new TMPro.TMP_Dropdown.OptionData("Join"));
                dropdownComponent.options.Add(new TMPro.TMP_Dropdown.OptionData("Observe"));
            }
            
            // On option clicked event
            dropdownComponent.onValueChanged.AddListener(delegate
            {
                if (dropdownComponent.value == 0) return;
                DropdownValueChanged(dropdownComponent, room);
            });

            roomRowTransform.gameObject.SetActive(true);
            uIRooms.Add(new UIRoom { room = room, gameObject = roomRowTransform.gameObject });
        });
    }

    // Room update in the UI list
    public void UpdateRoom(Room room)
    {
        UIRoom uiRoom = uIRooms.Find(x => x.room.roomId == room.roomId);

        if(uiRoom == null) return;

        UnityThread.executeInUpdate(() =>
        {
            uiRoom.room = room;
                
            if (room.roomOpponent != null)
            {
                uiRoom.gameObject.transform.Find("roomOpponent").GetComponent<TMPro.TextMeshProUGUI>().text = room.roomOpponent.username;
            }
            else
            {
                uiRoom.gameObject.transform.Find("roomOpponent").GetComponent<TMPro.TextMeshProUGUI>().text = "";
            }

            uiRoom.gameObject.transform.Find("roomStatus").GetComponent<TMPro.TextMeshProUGUI>().text = room.gameStatus.ToString();

            // Count number of players
            int players = 1;
            if (room.roomOpponent != null)
            {
                players = 2;
            }
            uiRoom.gameObject.transform.Find("roomPlayers").GetComponent<TMPro.TextMeshProUGUI>().text = players.ToString() + "/2";
            uiRoom.gameObject.transform.Find("roomObservers").GetComponent<TMPro.TextMeshProUGUI>().text = room.observers.Count.ToString();
        });
    }

    // Dropdown value changed
    public void DropdownValueChanged(TMPro.TMP_Dropdown change, Room room)
    {
        // Get the selected option
        string option = change.options[change.value].text;

        // Switch case
        switch (option)
        {
            case "Join":
                JoinRoom(room);
                break;
            case "Observe":
                ObserveRoom(room);
                break;
            case "Delete":
                DeleteRoom(room);
                break;
        }
        change.value = 0;
    }

    // Join room
    public void JoinRoom(Room room)
    {
        var data = new { room_id = room.roomId };
        
        SocketManager.Instance.socket.Emit("join_game", response =>
        {
            try
            {
                StatusResponse respons = response.GetValue<StatusResponse>(0);

                NotificationsManager.Instance.ShowNotification(respons.message, 3, respons.status);

                if (respons.status == "success")
                {
                    // Open the game scene. Execute on unity thread
                    UnityThread.executeInUpdate(() =>
                    {
                        // Determine side
                        UserData.Instance.playerSide = UserData.PlayerSide.Black;
                        if (room.roomOwnerSide == SideColor.Black) {
                            UserData.Instance.playerSide = UserData.PlayerSide.White;
                        }
                        SceneManager.LoadScene("GameScene");
                    });
                }
                
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }, data);
    }

    // Observe room
    public void ObserveRoom(Room room)
    {
        var data = new { room_id = room.roomId };
        
        SocketManager.Instance.socket.Emit("observe_game", response =>
        {
            try
            {
                StatusResponse respons = response.GetValue<StatusResponse>(0);

                NotificationsManager.Instance.ShowNotification(respons.message, 3, respons.status);
                if (respons.status == "success")
                {
                    // Subscribe to the room to receive events
                    SocketManager.Instance.socket.Emit("subscribe_to_room", response =>
                    {
                        var result = response.GetValue<StatusResponse>(0);

                        if (result.status == "success")
                        {
                            Debug.Log("Subscribed to room " + room.roomId);
                        }
                        else
                        {
                            Debug.Log("Failed to subscribe to room " + room.roomId);
                            return;
                        }

                    }, data);

                    // Open the game scene. Execute on unity thread
                    UnityThread.executeInUpdate(() =>
                    {
                        UserData.Instance.playerSide = UserData.PlayerSide.Observer;
                        SceneManager.LoadScene("GameScene");
                    });
                }
                
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }, data);
    }

    // Delete room
    public void DeleteRoom(Room room)
    {
        var data = new { room_id = room.roomId };
        SocketManager.Instance.socket.Emit("delete_room", data);
    }

    // On Create Room Open Panel Button Click
    public void OnCreateRoomOpenPanelButtonClick()
    {
        UnityThread.executeInUpdate(() =>
        {
            createRoomStatusMessageText.enabled = false;
        });
        createRoomPanel.SetActive(true);
    }

    // On Cancel Create Room Button Click
    public void OnCancelCreateRoomButtonClick()
    {
        // Hide the create room panel
        createRoomPanel.SetActive(false);
        
        // Reset the dropdown value
        teamColorDropdown.value = 0;
    }

    // On Create Room Button Click
    public void OnCreateRoomButtonClick()
    {
        var data = new {
            player_mode = PlayerMode.Standard, // Standard mode with 2 players on 2 different clients
            opponent = default(object), // == Null
            side = teamColorDropdown.value == 0 ? "white" : "black"
        };

        // Send data and parse the response to create a room
        SocketManager.Instance.socket.Emit("create_room", response =>
        {
            try
            {
                StatusResponse respons = response.GetValue<StatusResponse>(0);

                UpdateCreateRoomStatusMessage(respons.message, respons.status);

                if (respons.status == "success")
                {
                    Room room = JsonConvert.DeserializeObject<Room>(respons.data);
                    UserData.Instance.currentRoom = room;

                    // Open the game scene. Execute on unity thread
                    UnityThread.executeInUpdate(() =>
                    {
                        UserData.Instance.playerSide = UserData.PlayerSide.Black;
                        if (room.roomOwnerSide == SideColor.White) {
                            UserData.Instance.playerSide = UserData.PlayerSide.White;
                        }
                        SceneManager.LoadScene("GameScene");
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }, data);
    }

    private void UpdateCreateRoomStatusMessage(string message, string type = "error")
    {
        UnityThread.executeInUpdate(() =>
        {
            createRoomStatusMessageText.text = message;
            createRoomStatusMessageText.enabled = true;

            if (type == "error")
            {
                createRoomStatusMessageText.color = errorColor;
            }
            else
            {
                createRoomStatusMessageText.color = successColor;
            }
        });
    }

    // generate a message when the game shuts down or switches to another Scene
    void OnDestroy()
    {
        // Stop listening for events
        SocketManager.Instance.socket.Off("room_updated");
        SocketManager.Instance.socket.Off("room_created");
        SocketManager.Instance.socket.Off("room_deleted");

    }
}

public class UIRoom
{
    public Room room { get; set; }
    public GameObject gameObject { get; set; }
    
}