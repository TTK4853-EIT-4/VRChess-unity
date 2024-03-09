using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class StatusResponse
{
    [JsonProperty("status")]
    public string status { get; set; }

    [JsonProperty("message")]
    public string message { get; set; }
    
    [JsonProperty("data")]
    public string data { get; set; }
}