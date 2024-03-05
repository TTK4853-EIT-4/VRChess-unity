using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AuthManager : MonoBehaviour
{

    // Login Input Fields
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;

    // Register Input Fields
    public TMP_InputField registerFirstName;
    public TMP_InputField registerLastName;
    public TMP_InputField registerUsername;
    public TMP_InputField registerPassword;
    public TMP_InputField repeatPassword;

    // Error messages Texts
    public TMP_Text loginError;
    public TMP_Text registerError;
    private Color errorColor = new Color32(255, 114, 144, 255);
    private Color successColor = new Color32(147, 255, 114, 255);


    // Start is called before the first frame update
    void Start()
    {
        // Set error messages to empty
        loginError.text = "";
        registerError.text = "";
    }

    // TODO: Implement more extended validation (max length, special characters, etc.)
    public void OnLoginButtonClick()
    {
        string username = loginUsername.text.Trim();
        string password = loginPassword.text.Trim();

        if (username == "" || password == "")
        {
            UnityThread.executeInUpdate(() =>
            {
                loginError.color = errorColor;
                loginError.text = "Please fill in all fields";
            });
            return;
        }

        var data = new { username = username, password = password };

        SocketManager.Instance.socket.Emit("login", response =>
        {
            var result = response.GetValue<StatusResponse>(0);

            if (result.status.ToLower() != "success")
            {
                UnityThread.executeInUpdate(() =>
                {
                    loginError.color = errorColor;
                    loginError.text = result.message;
                });
            }
            else
            {
                UnityThread.executeInUpdate(() =>
                {
                    loginError.color = successColor;
                    loginError.text = result.message;
                });
            }
        }, data);
    }

    // TODO: Implement more extended validation (max length, special characters, etc.)
    public void OnRegisterButtonClick()
    {
        string firstName = registerFirstName.text.Trim();
        string lastName = registerLastName.text.Trim();
        string username = registerUsername.text.Trim();
        string password = registerPassword.text.Trim();
        string repeat = repeatPassword.text.Trim();

        if (firstName == "" || lastName == "" || username == "" || password == "" || repeat == "")
        {
            UnityThread.executeInUpdate(() =>
            {
                registerError.color = errorColor;
                registerError.text = "Please fill in all fields";
            });
            return;
        }

        if (password != repeat)
        {
            UnityThread.executeInUpdate(() =>
            {
                registerError.color = errorColor;
                registerError.text = "Passwords do not match";
            });
            return;
        }

        var data = new { firstname = firstName, lastname = lastName, username = username, password = password };

        SocketManager.Instance.socket.Emit("register", response =>
        {
            var result = response.GetValue<StatusResponse>(0);

            if (result.status.ToLower() != "success")
            {
                UnityThread.executeInUpdate(() =>
                {
                    registerError.color = errorColor;
                    registerError.text = result.message;
                });
            }
            else
            {
                UnityThread.executeInUpdate(() =>
                {
                    registerError.color = successColor;
                    registerError.text = result.message;
                });
            }
        }, data);
    }
}
