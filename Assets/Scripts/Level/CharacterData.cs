using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Data", menuName = "Data/Character")]
public class CharacterData : ScriptableObject
{
    [System.Serializable]
    public class CharacterPrefab
    {
        public string name = "Characer";
        public Character prefab;
        public int roomID;
    }

    public CharacterPrefab[] prefabs;

    public CharacterPrefab GetCharacterForRoomID(int id)
    {
        foreach (var prefab in prefabs)
        {
            if(prefab.roomID == id)
            {
                return prefab;
            }
        }

        throw new System.Exception("Character for room id " + id + " not found");
    }
}
