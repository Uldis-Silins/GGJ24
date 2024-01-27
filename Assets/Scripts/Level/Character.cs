using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public enum CharacterTeamType { None, Light, Dark }

    public GameObject meshObject;
    public Animator characterAnimator;
    public CharacterTeamType type;

    [SerializeField] private bool m_drawGizmos;

    private void OnDrawGizmos()
    {
        if (m_drawGizmos && meshObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(meshObject.transform.position, 0.22f);
        }
    }
}
