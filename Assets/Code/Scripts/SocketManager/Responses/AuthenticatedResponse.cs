using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class AuthenticatedResponse
{
    [JsonProperty("token")]
    public string token { get; set; }
}
