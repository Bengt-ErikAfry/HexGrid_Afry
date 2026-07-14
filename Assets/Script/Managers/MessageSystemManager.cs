using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class MessageSystemManager : MonoBehaviour
{
    public static MessageSystemManager Instance;

    [Header("Prefabs & References")]
    public RectTransform contentArea;
    public MessageSystemEntery entryPrefab;
    public List<MessageSystemEntery> messageList = new List<MessageSystemEntery>();


    [Header("Animation settings")]
    public float duration = 0.6f;
    public float startHeight = 0;
    public float endHeight = 0;
    public bool isExpanded = false;
    public RectTransform messageSystemRectTransform;
    public ScrollRect _scroll;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startHeight= messageSystemRectTransform.sizeDelta.y;
    }

    public void CreateMessage(string message, string hidenmessage ,Vector2 mapCoords, Color color)
    {
        MessageSystemEntery entry = Instantiate(entryPrefab, contentArea);
        entry.Setup(message, hidenmessage, mapCoords, color);
        messageList.Add(entry);
        // Force layout refresh
        ScrollToBottom();
    }

    public void MessageSystemToggle()
    {
        // toggle view
        isExpanded = !isExpanded;
        if(isExpanded)
        {
            PlayScale();
            GameStateMachine.Instance.SetState(GameplayStateId.UIOnly);
        }
        else
        {
            ReverseScale();
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
        }
        ScrollToBottom();
    }

    public void CollapseIfExpanded()
    {
        if (isExpanded)
        {
            MessageSystemToggle();
        }
    }

    public void ScrollToBottom()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea);
        _scroll.verticalNormalizedPosition = 0f;
    }

    [ContextMenu("Play Scale")]
    public void PlayScale()
    {
        if (messageSystemRectTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(AnimateHeightSmooth(startHeight, endHeight, duration));

        foreach (var entery in messageList)
        {
            entery.OnExpand();
        }

        // Force layout refresh
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea);
    }

    [ContextMenu("Reverse Scale")]
    public void ReverseScale()
    {
        if (messageSystemRectTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(AnimateHeightSmooth(endHeight, startHeight, duration));

        foreach (var entery in messageList)
        {
            entery.OnCollapse();
        }

        // Force layout refresh
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea);
    }


    public IEnumerator AnimateHeightSmooth(
        float from,
        float to,
        float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float e = Mathf.SmoothStep(0f, 1f, u); // ease in/out

            messageSystemRectTransform.sizeDelta = new Vector2(messageSystemRectTransform.sizeDelta.x, Mathf.Lerp(from, to, e));
            yield return null;
        }

        messageSystemRectTransform.sizeDelta = new Vector2(messageSystemRectTransform.sizeDelta.x, to);
    }

}
