using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

// Singleton class
public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    public string ServerAddress = "http://77.71.71.125:5000/";
    public SocketIOUnity socket;
    public string AuthToken = null;

    static void connect(string token = null) {
        // TODO: Validate the server address and port
        var uri = new Uri(Instance.ServerAddress);
        if (Instance.socket != null)
        {
            Instance.socket.Disconnect();
        } else {
            Instance.socket = new SocketIOUnity(uri, new SocketIOOptions
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
            SocketManager.Instance.socket.JsonSerializer = new NewtonsoftJsonSerializer();
        }

        if(token != null)
        {
            Instance.socket.Options.ExtraHeaders = new Dictionary<string, string>
            {
                {"Cookie", "AuthToken=" + token }
            };
        }

        // Print "Connecting..." if no token and "Connecting with token {token}..." if token is provided
        string logMessage = "Connecting" + (token == null ? "..." : " with token " + token + "...");
        Debug.Log(logMessage);
        Instance.socket.Connect();
    }

    // Start is called before the first frame update
    void Start()
    {
        SocketManager socketManager = SocketManager.Instance;
        if(socket == null)
        {
            connect(AuthToken);
        }

        ///// reserved socketio events
        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("socket.OnConnected");

            if (AuthToken != null)
            {
                socket.Emit("get_my_user", response =>
                {
                    var result = response.GetValue<User>(0);
                    UserData.Instance.loggedUser = result;
                });
            }
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

                AuthToken = data.token;
                connect(data.token);

                // Open the next scene which is not in the build settings
                SceneManager.LoadScene("RoomsScene", LoadSceneMode.Single);
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

    // Awake
    void Awake()
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
}