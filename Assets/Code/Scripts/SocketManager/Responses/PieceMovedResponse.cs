using Newtonsoft.Json;

// Move Class
[System.Serializable]
public class Move
{
    [JsonProperty("source")]
    public string source { get; set; }

    [JsonProperty("target")]
    public string target { get; set; }

    [JsonProperty("piece")]
    public string piece { get; set; }
}

[System.Serializable]
public class PieceMovedResponse
{
    [JsonProperty("move")]
    public Move move { get; set; }

    [JsonProperty("fen")]
    public string fen { get; set; }
}
