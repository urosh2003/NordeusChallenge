using UnityEngine;

[CreateAssetMenu(fileName = "NewEnvironment", menuName = "Game Data/Environment Data", order = 3)]
public class EnvironmentSO : ScriptableObject
{
    public string environmentId;
    public string displayName;
    public Sprite backgroundSprite;
}
