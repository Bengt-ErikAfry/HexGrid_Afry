using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class MessageSystemEntery : MonoBehaviour
{
    public TextMeshProUGUI textField;
    public string messageText;
    public string hidenMessageText;
    public Vector2 mapCoordsText;

    public void Awake()
    { 
        textField = this.GetComponent<TextMeshProUGUI>();
    }

    public void Setup(string message, string hidenMessage, Vector2 mapCoords, Color color)
    {
        if(textField == null)
        {
            return;
        }
        messageText = message;
        hidenMessageText = hidenMessage;
        mapCoordsText = mapCoords;
        textField.color = color;

        if(MessageSystemManager.Instance.isExpanded)
        {
            OnExpand();
        }
        else
        {
            OnCollapse();
        }

        // Hook up click event
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnExpand()
    {
        textField.text = messageText + "\n" +
        "<size=75%>" + hidenMessageText + "</size>";
        MessageSystemManager.Instance.ScrollToBottom();
    }

    public void OnCollapse()
    {
        textField.text = messageText;
        MessageSystemManager.Instance.ScrollToBottom();
    }

    void OnClick()
    {
        if(MessageSystemManager.Instance.isExpanded)
        {
            StartCoroutine(CameraManager.Instance.MoveCameraTo(new Vector3(mapCoordsText.x, mapCoordsText.y, Camera.main.transform.position.z), 1f, false));
        }
        else 
        {
            MessageSystemManager.Instance.MessageSystemToggle();
        }
    }
}