using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationsManager : MonoBehaviour
{
    // Singleton class instance
    public static NotificationsManager Instance { get; private set; }

    // UI Elements
    public TMPro.TextMeshProUGUI text;
    public Transform notificationPanel;
    private float notificationDuration = 3;
    private float notificationElapsed = 0;

    // Panel colors
    public Color32 infoColor = new Color32(57, 124, 226, 212);
    public Color32 successColor = new Color32(57, 226, 57, 212);
    public Color32 errorColor = new Color32(226, 57, 57, 212);
    public Color32 warningColor = new Color32(226, 226, 57, 212);
    public Color32 defaultColor = new Color32(0, 0, 0, 212);

    // On fixed update
    void FixedUpdate()
    {
        if (notificationPanel.gameObject.activeSelf)
        {
            notificationElapsed += Time.deltaTime;
            if (notificationElapsed >= notificationDuration)
            {
                notificationPanel.gameObject.SetActive(false);
                notificationElapsed = 0;
            }
        }
    }

    // ShowNotification
    public void ShowNotification(string message, float duration = 3, string type = "info")
    {
        UnityThread.executeInUpdate(() =>
        {
            text.text = message;

            // Update panel height
            var height = text.preferredHeight;
            notificationPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(notificationPanel.GetComponent<RectTransform>().sizeDelta.x, height);

            // Update panel color
            switch (type)
            {
                case "info":
                    notificationPanel.GetComponent<UnityEngine.UI.Image>().color = infoColor;
                    break;
                case "success":
                    notificationPanel.GetComponent<UnityEngine.UI.Image>().color = successColor;
                    break;
                case "error":
                    notificationPanel.GetComponent<UnityEngine.UI.Image>().color = errorColor;
                    break;
                case "warning":
                    notificationPanel.GetComponent<UnityEngine.UI.Image>().color = warningColor;
                    break;
                default:
                    // 000000, 212 transparent
                    notificationPanel.GetComponent<UnityEngine.UI.Image>().color = defaultColor;
                    break;
            }
            
            notificationPanel.gameObject.SetActive(true);

            notificationDuration = duration;
            notificationElapsed = 0;
        });
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

        notificationPanel.gameObject.SetActive(false);
    }
}
