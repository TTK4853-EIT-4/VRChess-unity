using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class RoomDeletedResponse
{
    [JsonProperty("room_id")]
    public string roomId { get; set; }
}
