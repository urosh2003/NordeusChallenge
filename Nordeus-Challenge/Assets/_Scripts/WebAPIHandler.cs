using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebAPIHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetText());
    }
    IEnumerator GetText() {
        UnityWebRequest request = UnityWebRequest.PostWwwForm("http://localhost:8080/api/games", "Hello World!");
        yield return request.SendWebRequest();
 
        if (request.result != UnityWebRequest.Result.Success) {
            Debug.Log(request.error);
        }
        else {
            // Show results as text
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("JSON: " + jsonResponse);
 
            try {
                GameResponse response = GameParser.Parse(jsonResponse);
                Debug.Log($"Parsed Game ID: {response.gameId}");
                Debug.Log($"Player HP: {response.state.player.currentHp}/{response.state.player.maxHp}");
                Debug.Log($"Enemy HP: {response.state.enemy.currentHp}/{response.state.enemy.maxHp}");
                Debug.Log($"Events count: {response.events?.Count ?? 0}");
            }
            catch (System.Exception e) {
                Debug.LogError("Failed to parse JSON: " + e.Message);
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
