using UnityEngine;

public class WinScreen : MonoBehaviour
{
    [SerializeField] GameObject winScreen;

    private void Awake()
    {
        GameManager.OnRunCompleted += ShowWinScreen;
    }
    private void OnDestroy()
    {
        GameManager.OnRunCompleted -= ShowWinScreen;
    }
    
    private void ShowWinScreen() => winScreen.SetActive(true);
}
