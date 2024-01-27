using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public enum CharacterTeamType { None, Light, Dark }
    public enum AnimationStateType { None, Idle }

    public GameObject meshObject;
    public Animator characterAnimator;
    public CharacterTeamType type;
    public int characterId;

    [SerializeField] private bool m_drawGizmos;

    private Rigidbody m_rigidbody;

    private Vector3 m_moveDirection;
    private Vector3 m_moveTarget;
    private bool m_moveEnabled;
    private bool m_checkGrounded;

    private readonly int m_idleAnimationHash = Animator.StringToHash("Idle");
    private readonly int m_winAnimationHash = Animator.StringToHash("Win");

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        characterAnimator.SetFloat(m_idleAnimationHash, characterId);
        m_rigidbody.isKinematic = false;
        m_rigidbody.useGravity = true;
        m_checkGrounded = true;
    }

    private void OnDrawGizmos()
    {
        if (m_drawGizmos)
        {
            if (meshObject != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 0.25f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(meshObject.transform.position, 0.22f);
            }
        }
    }

    private void Update()
    {
        m_moveDirection = Vector3.zero;

        if(Vector3.Distance(m_moveTarget, transform.position) > 0.05f)
        {
            m_moveDirection = (m_moveTarget - transform.position).normalized;
        }

        if (m_moveEnabled)
        {
            //m_rigidbody.MovePosition((transform.position + m_moveDirection) * Time.deltaTime);
            m_rigidbody.velocity = m_moveDirection * 2f * Time.deltaTime;
        }

        //if(m_checkGrounded)
        //{
        //    if(Physics.Raycast(transform.position, transform.position - Vector3.up * 0.1f, 1 << LayerMask.NameToLayer("Floor")))
        //    {
        //        m_rigidbody.isKinematic = true;
        //        m_rigidbody.useGravity = false;
        //    }

        //    m_checkGrounded = false;
        //}
    }

    public void SetMoveTarget(Vector3 position)
    {
        m_moveTarget = position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(m_checkGrounded &&
            collision.gameObject.layer == LayerMask.NameToLayer("Floor")
            && collision.collider.transform.position.y < transform.position.y)
        {
            m_rigidbody.useGravity = false;
            m_rigidbody.isKinematic = true;
            m_checkGrounded = false;
        }
    }

    public void SetWin()
    {
        characterAnimator.SetBool(m_winAnimationHash, true);
    }

    public void ResetWin()
    {
        characterAnimator.SetBool(m_winAnimationHash, false);
    }
}
