using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the node prefab. RunMapUI calls Setup() on each rebuild.
///
/// Prefab requirements:
///   - Button component
///   - Image component assigned to `icon`
/// </summary>
public class RunNodeButton : MonoBehaviour
{
    [Header("Icon")]
    public Image icon;

    [Header("Sprites")]
    public Sprite normalSprite;   // COMBAT node
    public Sprite bossSprite;     // BOSS node
    public Sprite shopSprite;     // SHOP node
    public Sprite restSprite;     // REST_SITE node
    public Sprite clearedSprite;  // any cleared node

    [Header("Colors")]
    public Color availableColor   = Color.white;
    public Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public enum State { Current, NextChoice, Cleared, Locked }

    private Button _button;

    // ── API ───────────────────────────────────────────────────────────────────

    public void Setup(RunNode node, RunResponse run, Action<RunNode> onClick)
    {
        _button ??= GetComponent<Button>();

        State state = ResolveState(node, run);

        if (icon != null)
        {
            icon.sprite = state == State.Cleared   ? clearedSprite
                        : node.Type == NodeType.BOSS       ? bossSprite
                        : node.Type == NodeType.SHOP       ? shopSprite
                        : node.Type == NodeType.REST_SITE  ? restSprite
                        :                                    normalSprite;

            icon.color = state == State.Locked ? unavailableColor : availableColor;
        }
    
        bool interactable = false;
        switch (state)
        {
            case State.Current:
                interactable = true;
                break;
            case State.NextChoice:
                interactable = true;
                break;
            case State.Cleared:
                if (node.id==run.currentNodeId)
                    interactable = true;
                break;
        }
        _button.interactable = interactable;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke(node));
    }

    // ── State resolution ──────────────────────────────────────────────────────

    private static State ResolveState(RunNode node, RunResponse run)
    {
        if (node.IsCleared && node.Type==NodeType.COMBAT) return State.Cleared;

        if (node.id == run.currentNodeId) return State.Current;

        var current = run.CurrentNode;
        if (current != null && current.IsCleared &&
            current.nextNodeIds != null && current.nextNodeIds.Contains(node.id))
            return State.NextChoice;

        return State.Locked;
    }
}
