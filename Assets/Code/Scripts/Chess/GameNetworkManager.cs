using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetwork : MonoBehaviour
{
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



        var data = new { room_id = UserData.Instance.currentRoom.roomId };
        // On game_start event
        SocketManager.Instance.socket.Emit("subscribe_to_room", response =>
        {
            var result = response.GetValue<StatusResponse>(0);

            if (result.status == "success")
            {
                Debug.Log("Subscribed to room " + data);
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
}
