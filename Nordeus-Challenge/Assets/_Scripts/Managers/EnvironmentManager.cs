using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }
    [SerializeField] GameObject plainsGrid;
    [SerializeField] GameObject bloodCryptGrid;
    [SerializeField] GameObject godlessShrineGrid;
    [SerializeField] GameObject templeGardenGrid;
    [SerializeField] GameObject overgrownTombGrid;
    
    void Awake()
    {
        Instance = this;
        
        SetEnvironment(GameManager.Instance?.CurrentEnvironment?.id);
    }
    
    public void SetEnvironment(string environmentId)
    {
        bloodCryptGrid.SetActive(environmentId == "bloodCrypt");
        godlessShrineGrid.SetActive(environmentId == "godlessShrine");
        templeGardenGrid.SetActive(environmentId == "templeGarden");
        overgrownTombGrid.SetActive(environmentId == "overgrownTomb");
        plainsGrid.SetActive(environmentId == "plains");
    }
    
}
