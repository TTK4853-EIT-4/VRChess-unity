using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class User
{
    [JsonProperty("id")]
    public int id { get; set; }

    [JsonProperty("username")]
    public string username { get; set; }

    [JsonProperty("firstname")]
    public string firstname { get; set; }

    [JsonProperty("lastname")]
    public string lastname { get; set; }

    [JsonProperty("last_online")]
    public string lastOnline { get; set; }
}
