using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Room : MonoBehaviour
{
    public int id;

    public Renderer roomRenderer;
    public Collider roomCollider;

    public CharacterData characterData;
    public Transform characterSpawnLocation;
    public Character character;

    public TextMeshPro debugText;
    public GameObject debugQuad;

    private Vector3 m_targetPosition;
    private float m_timer;
    private float m_moveTime = 0.5f;

    private bool m_inAnimation;

    public bool IsEmpty { get { return character == null; } }

    private void Start()
    {
        //debugText.text = id.ToString();
        //debugQuad.SetActive(type == RoomType.Triangle);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if(characterSpawnLocation != null)
        {
            Gizmos.DrawSphere(characterSpawnLocation.position, 0.05f);
        }
    }

    public void ToggleInteractable(bool isInteractable)
    {
        roomRenderer.enabled = isInteractable;
        roomCollider.enabled = isInteractable;
        //debugText.enabled = isInteractable;
    }

    private void Update()
    {
        if(m_inAnimation)
        {
            transform.position = Vector3.Lerp(transform.position, m_targetPosition, 1f - (m_timer / m_moveTime));
            m_timer -= Time.deltaTime;

            if(m_timer < 0f)
            {
                m_inAnimation = false;
            }
        }
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        m_targetPosition = targetPosition;
        m_timer = m_moveTime;
        m_inAnimation = true;
    }

    public void SpawnCharacter()
    {
        CharacterData.CharacterPrefab prefabData = characterData.GetCharacterForRoomID(id);
        var instance = Instantiate(prefabData.prefab, transform);
        character = instance;

        character.transform.position = characterSpawnLocation.position;
        character.transform.rotation = characterSpawnLocation.rotation;
    }
}
