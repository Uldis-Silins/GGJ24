using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Room : MonoBehaviour
{
    public int id;

    public Renderer roomRenderer;
    public Collider roomCollider;
    public Collider floorCollider;

    public CharacterData characterData;
    public Transform characterSpawnLocation;
    public Character character;
    public Transform idlePoint;

    public TextMeshPro debugText;
    public GameObject debugQuad;

    private Vector3 m_targetPosition;
    private float m_timer;
    private float m_moveTime = 0.5f;

    private float m_lockTimer = 2f;

    public bool IsEmpty { get { return character == null; } }
    public bool CanMove { get { return !IsLocked && (character != null && !character.IsBusy); } }
    public bool InAnimation { get; private set; }
    public bool IsLocked { get; private set; }

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

        if (!isInteractable) floorCollider.enabled = false;
        //debugText.enabled = isInteractable;
    }

    private void Update()
    {
        if(InAnimation)
        {
            transform.position = Vector3.Lerp(transform.position, m_targetPosition, 1f - (m_timer / m_moveTime));
            m_timer -= Time.deltaTime;

            if(m_timer < 0f)
            {
                InAnimation = false;
            }
        }

        if(IsLocked)
        {
            if(m_lockTimer < 0f)
            {
                IsLocked = false;
            }

            m_lockTimer -= Time.deltaTime;
        }
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        m_targetPosition = targetPosition;
        m_timer = m_moveTime;
        InAnimation = true;
    }

    public void SetLocked(float lockTime = 2f)
    {
        m_lockTimer = lockTime;
        IsLocked = true;
    }

    public void SpawnCharacter()
    {
        CharacterData.CharacterPrefab prefabData = characterData.GetCharacterForRoomID(id);
        var instance = Instantiate(prefabData.prefab, transform);
        character = instance;

        character.transform.position = characterSpawnLocation.position;
        character.transform.rotation = characterSpawnLocation.rotation;
        character.idlePoint = idlePoint;
    }

    public void PlayAttackSequence(Room targetRoom)
    {
        StartCoroutine(PlayAttackAfterAnimation(targetRoom));
        targetRoom.SetLocked();
    }

    private IEnumerator PlayAttackAfterAnimation(Room targetRoom)
    {
        while (InAnimation || targetRoom.InAnimation)
        {
            yield return null;
        }

        Vector3 targetDir = (transform.position - targetRoom.character.transform.position).normalized;
        targetDir.y = 0f;

        if (Mathf.Abs(targetDir.x) < 0.1f)
        {
            targetDir.z = 0.7f;

            if (targetDir.y < 0f)
            {
                targetDir.z = -0.7f;
            }
        }

        targetDir.x *= 0.5f;

        character.StartAttackSequence(targetRoom.character, targetRoom.character.transform.position + targetDir, 1f);
    }
}
