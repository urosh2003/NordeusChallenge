using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpringTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetText());
    }
    IEnumerator GetText() {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:8080/api/test");
        yield return request.SendWebRequest();
 
        if (request.result != UnityWebRequest.Result.Success) {
            Debug.Log(request.error);
        }
        else {
            // Show results as text
            Debug.Log(request.downloadHandler.text);
 
            // Or retrieve results as binary data
            byte[] results = request.downloadHandler.data;
            Debug.Log(System.Text.Encoding.UTF8.GetString(results));
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
