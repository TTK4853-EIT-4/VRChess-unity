using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class RoomListManager : MonoBehaviour
{

    private Transform roomListContainer;
    private Transform roomRowTemplate;

    private List<UIRoom> uIRooms = new List<UIRoom>();

    // On Awake
    private void Awake()
    {
        try
        {
            roomListContainer = GameObject.Find("roomListContainer").transform;
            roomRowTemplate = roomListContainer.Find("roomRowTemplate");

            // Hide the template
            roomRowTemplate.gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
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
            Debug.Log("Socket is not connected");
            // Wait for the socket to connect
            SocketManager.Instance.socket.OnConnected += (sender, e) =>
            {
                Debug.Log("socket.OnConnected");
                GetAllRooms();
            };
        }

        // On room_created event
        SocketManager.Instance.socket.On("room_created", response =>
        {
            Debug.Log("Room created");
            Room room = JsonConvert.DeserializeObject<Room>(response.GetValue<string>(0));
            AddRoom(room);
        });

        // On room_deleted event
        SocketManager.Instance.socket.On("room_deleted", response =>
        {
            Debug.Log("Room deleted");
            
            // The format is {"room_id": "ksjdhaskjhas"}
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

        // On room_updated event
        SocketManager.Instance.socket.On("room_updated", response =>
        {
            Debug.Log("Room updated");
            Room room = JsonConvert.DeserializeObject<Room>(response.GetValue<string>(0));
            UpdateRoom(room);
        });
    }

    // Get all rooms
    public void GetAllRooms()
    {
        Debug.Log("Getting all rooms");
        SocketManager.Instance.socket.Emit("get_all_rooms", response =>
        {
            try
            {
                string respons = response.GetValue<string>(0);
                List<Room> items = JsonConvert.DeserializeObject<List<Room>>(respons);

                foreach (var room in items)
                {
                    Debug.Log(room.roomOwner.username);
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

            if(UserData.Instance.loggedUser.id == room.roomOwner.id)
            {
                // Add - Delete - option
                dropdownComponent.options.Add(new TMPro.TMP_Dropdown.OptionData("Delete"));
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

        if (option == "Join")
        {
            Debug.Log("Join room");
            //JoinRoom(room);
        }
        else if (option == "Observe")
        {
            Debug.Log("Observe room");
            //ObserveRoom(room);
        }
        else if (option == "Delete")
        {
            Debug.Log("Delete room");
            DeleteRoom(room);
        }

        Debug.Log(room.roomId);

        change.value = 0;
    }

    // Delete room
    public void DeleteRoom(Room room)
    {
        var data = new { room_id = room.roomId };
        SocketManager.Instance.socket.Emit("delete_room", data);
    }
}


public class UIRoom
{
    public Room room { get; set; }
    public GameObject gameObject { get; set; }
    
}
