using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class HttpHelpers
{
    public static IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError)
    {
        using var req = BuildRequest(url, "GET", null);
        yield return req.SendWebRequest();
        HandleResponse(req, onSuccess, onError);
    }

    public static IEnumerator Post(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        using var req = BuildRequest(url, "POST", json ?? "{}");
        yield return req.SendWebRequest();
        HandleResponse(req, onSuccess, onError);
    }

    public static IEnumerator Put(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        using var req = BuildRequest(url, "PUT", json);
        yield return req.SendWebRequest();
        HandleResponse(req, onSuccess, onError);
    }

    private static UnityWebRequest BuildRequest(string url, string method, string json)
    {
        var req = new UnityWebRequest(url, method);
        if (json != null)
        {
            var raw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(raw) { contentType = "application/json" };
        }
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");
        return req;
    }

    private static void HandleResponse(UnityWebRequest req, Action<string> onSuccess, Action<string> onError)
    {
        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[API] {req.method} {req.url} → {req.responseCode}");
            onSuccess(req.downloadHandler.text);
        }
        else
        {
            string err = $"{req.method} {req.url} failed ({req.responseCode}): {req.error}\n{req.downloadHandler?.text}";
            Debug.LogError($"[API] {err}");
            onError(err);
        }
    }
}
