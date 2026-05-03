using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen panel shown when the player starts a new run.
/// Displays one ClassCard per ClassSO registered in SpriteManager.
/// Attach to a panel in the MainMenu scene, starts inactive.
///
/// Scene setup
/// ───────────
///   ClassSelectionPanel (this script)
///     CardContainer   — Horizontal/Grid layout group
///     ConfirmButton   — disabled until a class is chosen
///     CancelButton    — returns to main menu
///     StatusText      — loading / error feedback
/// </summary>
public class ClassSelectionPanel : MonoBehaviour
{
    public static ClassSelectionPanel Instance { get; private set; }

    [SerializeField] Transform       cardContainer;
    [SerializeField] GameObject      cardPrefab;
    [SerializeField] Button          confirmButton;
    [SerializeField] Button          cancelButton;
    [SerializeField] TextMeshProUGUI statusText;

    private ClassSO              _selected;
    private List<(ClassSO, ClassCard)> _cards = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _selected = null;
        if (confirmButton) confirmButton.interactable = false;
        SetStatus(string.Empty);
        BuildCards();
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    // ── Button callbacks ──────────────────────────────────────────────────────

    public void OnConfirm()
    {
        if (_selected == null) return;
        SetStatus("Creating run...");
        if (confirmButton) confirmButton.interactable = false;
        if (cancelButton)  cancelButton.interactable  = false;
        MainMenuManager.Instance.OnStartNewRun(_selected.classId);
    }

    public void OnCancel() => Close();

    // ── Cards ─────────────────────────────────────────────────────────────────

    private void BuildCards()
    {
        foreach (var (_, card) in _cards) if (card) Destroy(card.gameObject);
        _cards.Clear();

        foreach (var cls in SpriteManager.Instance.GetAllClasses())
        {
            var go   = Instantiate(cardPrefab, cardContainer);
            var card = go.GetComponent<ClassCard>();
            card.Setup(cls, OnClassSelected);
            _cards.Add((cls, card));
        }
    }

    private void OnClassSelected(ClassSO cls)
    {
        _selected = cls;
        foreach (var (data, card) in _cards)
            card.SetSelected(data == cls);
        if (confirmButton) confirmButton.interactable = true;
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
