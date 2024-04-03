using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetwork : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
