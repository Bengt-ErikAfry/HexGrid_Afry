
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class MessageBoxManager : MonoBehaviour
{

    public static MessageBoxManager Instance;

    [Header("Reference")]
    public GameObject messageBox;
    public TMP_Text messageText;

    public Button okButton;
    public Button yesButton;
    public Button noButton;

    private void Awake()
    {
        Instance = this;
        messageBox.SetActive(false);
    }

    // ✅ Simple OK box
    public void ShowMessage(string message)
    {
        ResetButtons();

        messageText.text = message;

        okButton.gameObject.SetActive(true);
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(Hide);

        messageBox.SetActive(true);

        RectTransform rt = messageBox.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
    }

    // ✅ Future expansion
    public void ShowChoice(string message, Action onYes, Action onNo)
    {
        ResetButtons();

        messageText.text = message;

        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);

        yesButton.onClick.AddListener(() =>
        {
            onYes?.Invoke();
            Hide();
        });

        noButton.onClick.AddListener(() =>
        {
            onNo?.Invoke();
            Hide();
        });

        messageBox.SetActive(true);

        RectTransform rt = messageBox.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
    }

    void Hide()
    {
        messageBox.SetActive(false);
    }

    void ResetButtons()
    {
        okButton.onClick.RemoveAllListeners();
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        okButton.gameObject.SetActive(false);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }
}
