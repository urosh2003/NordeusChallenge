using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One selectable class card in the class selection screen.
/// Prefab: Image (portrait), TMP labels, Button.
/// </summary>
public class ClassCard : MonoBehaviour
{
    [SerializeField] Image           portrait;
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] TextMeshProUGUI descriptionLabel;
    [SerializeField] TextMeshProUGUI statsLabel;
    [SerializeField] TextMeshProUGUI movesLabel;
    [SerializeField] Button          selectButton;
    [SerializeField] GameObject      selectedBorder;  // highlight when chosen

    private ClassSO  _data;
    private Action<ClassSO> _onSelect;

    public void Setup(ClassSO data, Action<ClassSO> onSelect)
    {
        _data     = data;
        _onSelect = onSelect;

        if (portrait)     portrait.sprite = data.portrait;
        if (nameLabel)    nameLabel.text  = data.className;
        if (descriptionLabel) descriptionLabel.text = data.description;

        if (statsLabel)
            statsLabel.text =
                $"HEALTH {data.baseHealth}  ATTACK {data.baseAttack}  DEFENSE {data.baseDefense}  MAGIC {data.baseMagic}";

        if (movesLabel && data.startingMoveNames != null)
            movesLabel.text = string.Join(" · ", data.startingMoveNames);

        selectButton?.onClick.RemoveAllListeners();
        selectButton?.onClick.AddListener(OnSelectClicked);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedBorder) selectedBorder.SetActive(selected);
    }

    private void OnSelectClicked() => _onSelect?.Invoke(_data);
}
