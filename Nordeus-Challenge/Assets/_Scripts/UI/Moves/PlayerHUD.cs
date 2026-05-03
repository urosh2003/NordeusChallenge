using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public RectTransform container;
    [SerializeField] GameObject moveButtonPrefab;
    [SerializeField] GameObject passButtonPrefab;
    
    private readonly List<GameObject> _spawned   = new();
    private List<string> _equippedMoves;

    
    
    void OnEnable()
    {
        GameManager.OnPlayerStateUpdated += Build;
        if (GameManager.Instance.PlayerState != null) Build(GameManager.Instance.PlayerState);
    }

    void OnDisable()
    {
        GameManager.OnPlayerStateUpdated -= Build;
    }

    void Build(PlayerStateResponse playerState)
    {
        if (_equippedMoves != null && _equippedMoves.SequenceEqual(playerState.equippedMoves)) return;
        Clear();
        
        _equippedMoves = playerState.equippedMoves;

        foreach (var moveId in _equippedMoves)
        {
            var go = Instantiate(moveButtonPrefab, container);
            go.GetComponent<MoveButton>()?.Setup(moveId, id => OnMoveActivated(id));
            _spawned.Add(go);
        }
        var passButton = Instantiate(passButtonPrefab, container);
        _spawned.Add(passButton);
    }

    private void OnMoveActivated(string moveId)
    {
        GameManager.Instance.PlayerAction(moveId,
            combat => Debug.Log($"PlayerMove({moveId}) OK"),
            err    => Debug.Log($"PlayerMove({moveId}) " + err));
    }
    
    private void Clear()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
    }
}
