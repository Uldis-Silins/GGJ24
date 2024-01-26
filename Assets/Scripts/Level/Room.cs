using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Room : MonoBehaviour
{
    public enum RoomType { None, Circle, Triangle }

    public RoomType type;
    public int id;

    public Renderer roomRenderer;
    public Collider roomCollider;

    public TextMeshPro debugText;
    public GameObject debugQuad;

    private Vector3 m_targetPosition;
    private float m_timer;
    private float m_moveTime = 0.5f;

    private void Start()
    {
        debugText.text = id.ToString();
        debugQuad.SetActive(type == RoomType.Triangle);
    }

    public void ToggleInteractable(bool isInteractable)
    {
        roomRenderer.enabled = isInteractable;
        roomCollider.enabled = isInteractable;
        debugText.enabled = isInteractable;
    }

    private void Update()
    {
        if(m_timer > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, m_targetPosition, 1f - (m_timer / m_moveTime));
            m_timer -= Time.deltaTime;
        }
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        m_targetPosition = targetPosition;
        m_timer = m_moveTime;
    }
}
