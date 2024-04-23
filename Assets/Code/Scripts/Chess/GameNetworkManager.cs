using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class GameNetwork : MonoBehaviour
{

    public static event Action GameDataUpdatedEvent;

    // Start is called before the first frame update
    void Start()
    {
        
        
    }

    // Awake
    private void Awake()
    {
        // On piece_moved event
        SocketManager.Instance.socket.On("piece_moved_", (data) =>
        {
            Debug.Log("Piece moved event received");
            Debug.Log(data);

            try
            {
                PieceMovedResponse move = data.GetValue<PieceMovedResponse>(0);

                string fen = move.fen;

                // Execute on unity main thread
                UnityThread.executeInUpdate(() =>
                {
                    GameManager.Instance.LoadGame(fen);
                });
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        });

        // On room_updated_ event
        SocketManager.Instance.socket.On("room_updated_", (data) =>
        {
            Debug.Log("Room updated event received");
            Debug.Log(data);

            try
            {
                Room room = JsonConvert.DeserializeObject<Room>(data.GetValue<string>(0));

                // Execute on unity main thread
                UnityThread.executeInUpdate(() =>
                {
                    UserData.Instance.currentRoom = room;
                    GameDataUpdatedEvent?.Invoke();

                    if (room.gameStatus == GameStatus.ENDED)
                    {
                        NotificationsManager.Instance.ShowNotification("Game Over!", 3, "info");
                    }

                });
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        });



        var data = new { room_id = UserData.Instance.currentRoom.roomId };
        // On game_start event
        SocketManager.Instance.socket.Emit("subscribe_to_room", response =>
        {
            var result = response.GetValue<StatusResponse>(0);

            if (result.status == "success")
            {
                Room room = JsonConvert.DeserializeObject<Room>(result.data);
                UserData.Instance.currentRoom = room;
                Debug.Log("Subscribed to room " + data + ", room observers: " + room.observers.Count);
                GameDataUpdatedEvent?.Invoke();
            }
            else
            {
                Debug.Log("Failed to subscribe to room " + data);
                return;
            }

        }, data);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // generate a message when the game shuts down or switches to another Scene
    void OnDestroy()
    {
        
    }
}
