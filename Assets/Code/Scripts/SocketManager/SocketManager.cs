using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class SocketManager : MonoBehaviour
{
    public string ServerAddress = "http://77.71.71.125:5000/";
    public SocketIOUnity socket;
    public string AuthToken = null;

    void connect(string token = null) {
        // TODO: Validate the server address and port
        var uri = new Uri(ServerAddress);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
                {
                    {"token", "UNITY" }
                }
            ,
            EIO = 4
            ,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Reconnection = true,
            ReconnectionDelay = 1000,
            ReconnectionDelayMax = 5000,
            ReconnectionAttempts = 99999
            
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        if(token != null)
        {
            socket.Options.ExtraHeaders = new Dictionary<string, string>
            {
                {"Cookie", "AuthToken=" + token }
            };
        }

        // Print "Connecting..." if no token and "Connecting with token {token}..." if token is provided
        string logMessage = "Connecting" + (token == null ? "..." : " with token " + token + "...");
        Debug.Log(logMessage);
        socket.Connect();
    }

    // Start is called before the first frame update
    void Start()
    {
        connect();

        ///// reserved socketio events
        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("socket.OnConnected");
        };

        socket.OnPing += (sender, e) =>
        {
            //Debug.Log("Ping");
        };

        socket.OnPong += (sender, e) =>
        {
            //Debug.Log("Pong: " + e.TotalMilliseconds);
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("disconnect: " + e);
        };

        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}");
        };
        ////

        // On Authenticated event
        socket.OnUnityThread("authenticated", (responseData) =>
        {
            try
            {
                var data = responseData.GetValue<AuthenticatedResponse>(0);
                Debug.Log(data.token);

                AuthToken = data.token;
                connect(data.token);

                // Open the next scene which is not in the build settings
                //SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            } catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }  
        });
    }

    // Emit Login
    public void EmitLogin()
    {
        // Emit with username test and password test
        socket.Emit("login", new { username = "test", password = "test" });
    }

    // Emit Test
    public void EmitTest()
    {
        string data = "Hello World";

        // Send data and parse the response
        socket.Emit("test", response =>
        {
            var result = response.GetValue<StatusResponse>(0);
            
        }, data);
    }

    public static bool IsJSON(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) { return false; }
        str = str.Trim();
        if ((str.StartsWith("{") && str.EndsWith("}")) || //For object
            (str.StartsWith("[") && str.EndsWith("]"))) //For array
        {
            try
            {
                var obj = JToken.Parse(str);
                return true;
            }catch (Exception ex) //some other exception
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}