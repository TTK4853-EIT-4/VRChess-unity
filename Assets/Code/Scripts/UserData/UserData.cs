using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserData : MonoBehaviour
{
    public static UserData Instance { get; private set; }

    public User loggedUser { get; set; }
    public Room currentRoom { get; set; }
    public User opponentUser { get; set; }

    // Specifying the player side (White, Black or Observer)
	public PlayerSide playerSide { get; set; } = PlayerSide.Observer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Enum for the type of the user (White, Black or Observer)
	public enum PlayerSide {
        Observer,
        Black,
		White
	}
}
