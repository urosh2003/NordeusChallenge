using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaySceneMenu : MonoBehaviour
    {
        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void OpenMenu()
        {
            gameObject.SetActive(true);
        }
    }
