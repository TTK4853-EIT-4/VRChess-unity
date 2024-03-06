using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum GameStatus
{
    Waiting = 1,
    Playing = 2,
    Finished = 3
}

[System.Serializable]
public class Room
{
    [JsonProperty("room_id")]
    public string roomId { get; set; }

    [JsonProperty("room_owner")]
    public User roomOwner { get; set; }

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
}