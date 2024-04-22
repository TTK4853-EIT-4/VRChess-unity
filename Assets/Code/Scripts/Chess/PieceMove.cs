using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class SendPieceMovedData
{
    public PieceMove pieceMove { get; set; }

    public VisualPiece visualPiece { get; set; }

    public UnityChess.Piece piece { get; set; }
}

[System.Serializable]
public class PieceMove
{
    [JsonProperty("room_id")]
    public string room_id { get; set; }

    [JsonProperty("color")]
    public string color { get; set; }

    [JsonProperty("move")]
    public MoveData move { get; set; }


}

[System.Serializable]
public class MoveData {
    [JsonProperty("source")]
    public string source { get; set; }

    [JsonProperty("target")]
    public string target { get; set; }

    [JsonProperty("piece")]
    public string piece { get; set; }

    [JsonProperty("promotedPiece")]
    public string promotedPiece { get; set; }
}
