using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveButton : MonoBehaviour
{
    [Header("Icon")]
    public Image icon;
    private Button _button;
    private MoveSO _moveSO;

    
    public void Setup(string moveId, Action<string> onClick)
    {
        _button ??= GetComponent<Button>();
        
        _moveSO = SpriteManager.Instance.GetMoveSOById(moveId);
        
        icon.sprite = _moveSO.moveIcon;
        
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke(moveId));
    }
    
}
