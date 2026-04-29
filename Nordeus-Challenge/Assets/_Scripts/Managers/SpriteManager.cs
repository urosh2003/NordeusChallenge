using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance { get; private set; }

    [SerializeField] List<CharacterSO> charactersSOs;
    [SerializeField] List<MoveSO> movesSOs;

    public CharacterSO GetCharacterSOById(string characterId) =>
        charactersSOs.Find(so => so.characterId == characterId);
    
    public MoveSO GetMoveSOById(string moveId) =>
        movesSOs.Find(so => so.moveId == moveId);
    
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
