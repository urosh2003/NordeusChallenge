using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebAPIHandler : MonoBehaviour
{
    private GameResponse _gameResponse;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetText());
    }
    IEnumerator GetText()
    {
        string uri = "http://localhost:8080/api/games";

        // if no body is needed, you can send empty JSON {}
        string json = "{}";

        UnityWebRequest request = new UnityWebRequest(uri, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.uploadHandler.contentType = "application/json";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("JSON: " + jsonResponse);

            try
            {
                GameResponse response = GameParser.Parse(jsonResponse);
                _gameResponse = response;

                Debug.Log($"Parsed Game ID: {response.gameId}");
                Debug.Log($"Player HP: {response.state.player.currentHp}/{response.state.player.maxHp}");
                Debug.Log($"Enemy HP: {response.state.enemy.currentHp}/{response.state.enemy.maxHp}");
                Debug.Log($"Events count: {response.events?.Count ?? 0}");

                if (response.events != null)
                {
                    foreach (var evt in response.events)
                        Debug.Log(GameParser.FormatEvent(evt));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse JSON: " + e.Message);
            }
        }
    }

    public void StartAttack()
    {
        StartCoroutine(Attack());
    }
    IEnumerator Attack()
    {
        string url = "http://localhost:8080/api/games/" + _gameResponse.gameId + "/actions";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        string json = JsonUtility.ToJson(new MoveRequest("slash"));
        Debug.Log(json);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.uploadHandler.contentType = "application/json";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        Debug.Log(url);
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
                _gameResponse = response;
                Debug.Log($"Parsed Game ID: {response.gameId}");
                Debug.Log($"Player HP: {response.state.player.currentHp}/{response.state.player.maxHp}");
                Debug.Log($"Enemy HP: {response.state.enemy.currentHp}/{response.state.enemy.maxHp}");
                Debug.Log($"Events count: {response.events?.Count ?? 0}");

                if (response.events != null)
                {
                    foreach (var evt in response.events)
                        Debug.Log(GameParser.FormatEvent(evt));
                }
            }
            catch (System.Exception e) {
                Debug.LogError("Failed to parse JSON: " + e.Message);
            }
        }
    }
    public void StartEnemyTurn()
    {
        StartCoroutine(EnemyTurn());
    }
    IEnumerator EnemyTurn()
    {
        UnityWebRequest request =
            UnityWebRequest.Get("http://localhost:8080/api/games/" + _gameResponse.gameId + "/enemy-turn");
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
                _gameResponse = response;
                Debug.Log($"Parsed Game ID: {response.gameId}");
                Debug.Log($"Player HP: {response.state.player.currentHp}/{response.state.player.maxHp}");
                Debug.Log($"Enemy HP: {response.state.enemy.currentHp}/{response.state.enemy.maxHp}");
                Debug.Log($"Events count: {response.events?.Count ?? 0}");

                if (response.events != null)
                {
                    foreach (var evt in response.events)
                        Debug.Log(GameParser.FormatEvent(evt));
                }
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
