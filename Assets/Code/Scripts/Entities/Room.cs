using System.Collections.Generic;
using Newtonsoft.Json;

public enum GameStatus
{
    Waiting = 1,
    Playing = 2,
    Finished = 3
}

public enum PlayerMode
{
    Standard = 1, // # Standard multiplayer game with two players on different devices/clients
    Board_two_players = 2 // Two players on the same physical board
    // For BOARD_TWO_PLAYERS the opponent will be set by username on the room creation
}

public enum SideColor
{
    White = 0,
    Black = 1
}

[System.Serializable]
public class Room
{
    [JsonProperty("room_id")]
    public string roomId { get; set; }

    [JsonProperty("room_owner")]
    public User roomOwner { get; set; }

    [JsonProperty("room_owner_side")]
    public SideColor roomOwnerSide { get; set; }

    [JsonProperty("room_opponent")]
    public User roomOpponent { get; set; }

    [JsonProperty("observers")]
    public List<User> observers { get; set; }

    [JsonProperty("game_status")]
    public GameStatus gameStatus { get; set; }

    [JsonProperty("game_winner")]
    public User gameWinner { get; set; }
    
    [JsonProperty("game_loser")]
    public User gameLoser { get; set; }

    [JsonProperty("game")]
    public string game { get; set; }

    [JsonProperty("player_mode")]
    public PlayerMode playerMode { get; set; }
}