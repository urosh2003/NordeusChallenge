using UnityEngine;

[CreateAssetMenu(fileName = "NewEnvironment", menuName = "Game Data/Environment Data", order = 3)]
public class EnvironmentSO : ScriptableObject
{
    public string     environmentId;
    public GameObject tilemapRoot;
}
