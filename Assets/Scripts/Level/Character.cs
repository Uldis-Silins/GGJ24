using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Character : MonoBehaviour
{
    public struct AttackSequence
    {
        public Character target;
        public Vector3 attackPosition;
        public float attackTime;
    }

    public enum CharacterTeamType { None, Light, Dark }
    public enum StateType { None, Idle, Attack, Move }

    private delegate void StateHandler();

    public GameObject meshObject;
    public Animator characterAnimator;
    public CharacterTeamType type;
    public int characterId;

    public GameObject ragdoll;

    [SerializeField] private bool m_drawGizmos;

    private Rigidbody m_rigidbody;
    private bool m_ragdollSpawned;

    private StateHandler m_stateHandler;
    private StateType m_currentState;
    private Dictionary<StateType, StateHandler> m_states;
    private Queue<StateType> m_stateQueue;

    public Transform idlePoint;
    private Quaternion m_idleRotation;

    private float m_attackTimer;
    private float m_moveTimer;

    private Vector3 m_fromMovePos;
    private Vector3 m_moveTarget;
    private bool m_checkGrounded;

    private AttackSequence m_attackSequence;

    private readonly int m_idleAnimationHash = Animator.StringToHash("Idle");
    private readonly int m_winAnimationHash = Animator.StringToHash("Win");

    public bool IsBusy { get { return m_currentState != StateType.Idle; } }

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();

        m_states = new Dictionary<StateType, StateHandler>()
        {
            { StateType.None, () => { throw new System.Exception("Cannot be state None"); } },
            { StateType.Idle, EnterState_Idle },
            { StateType.Attack, EnterState_Attack },
            { StateType.Move, EnterState_Move }
        };

        m_currentState = StateType.Idle;
        m_stateHandler = EnterState_Idle;

        m_stateQueue = new Queue<StateType>();
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
                Gizmos.DrawWireSphere(idlePoint.position, 0.15f);
            }
        }
    }

    private void Update()
    {
        if(m_stateHandler != null)
        {
            m_stateHandler();
        }
    }

    private void ChangeState(StateType type)
    {
        m_currentState = type;
    }

    private void AddStateToQueue(StateType type)
    {
        m_stateQueue.Enqueue(type);
    }

    private StateHandler GetNextState()
    {
        if(m_stateQueue.Count > 0)
        {
            m_currentState = m_stateQueue.Dequeue();
            return m_states[m_currentState];
        }

        m_currentState = StateType.Idle;
        return m_states[StateType.Idle];
    }

    #region States
    private void EnterState_Idle()
    {
        if (m_checkGrounded) return;

        if(Vector3.Distance(transform.position, idlePoint.position) > 0.05f)
        {
            SetMoveTarget(idlePoint.position);
            m_currentState = StateType.Move;
            ExitState_Idle(EnterState_Move);
            return;
        }

        transform.SetPositionAndRotation(idlePoint.position, m_idleRotation);

        m_stateHandler = State_Idle;
    }

    private void State_Idle()
    {
        if(m_stateQueue.Count > 0)
        {
            ExitState_Idle(GetNextState());
        }

        if(m_currentState != StateType.Idle)
        {
            ExitState_Idle(m_states[m_currentState]);
        }
    }

    private void ExitState_Idle(StateHandler targetState)
    {
        m_stateHandler = targetState;
    }

    private void EnterState_Attack()
    {
        characterAnimator.SetTrigger("Attack");
        m_stateHandler = State_Attack;
    }

    private void State_Attack()
    {
        Vector3 dir = (m_attackSequence.target.transform.position - transform.position).normalized;
        dir.y = 0;
        dir.z = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);

        if (m_currentState != StateType.Attack)
        {
            ExitState_Attack(m_states[m_currentState]);
        }

        //transform.position = m_attackSequence.targetPosition;

        if(m_attackTimer <= 0.5f)
        {
            m_attackSequence.target.SpawnRagdoll(transform.position);
        }

        if (m_attackTimer <= 0f)
        {
            ExitState_Attack(GetNextState());
        }

        m_attackTimer -= Time.deltaTime;
    }

    private void ExitState_Attack(StateHandler targetState)
    {
        m_attackSequence.target.DespawnRagdoll();
        characterAnimator.ResetTrigger("Attack");
        m_stateHandler = targetState;
    }

    private void EnterState_Move()
    {
        characterAnimator.SetBool("Move", true);
        m_stateHandler = State_Move;
    }

    private void State_Move()
    {
        Vector3 moveDir = (m_moveTarget - transform.position).normalized;
        characterAnimator.SetFloat("x", moveDir.x);
        characterAnimator.SetFloat("y", moveDir.y);
        Vector3 lookDir = moveDir;
        lookDir.y = lookDir.z = 0f;
        if (Mathf.Abs(lookDir.x) > 0)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }

        transform.position = Vector3.Lerp(m_fromMovePos, m_moveTarget, m_moveTimer / 1f);

        if (m_moveTimer >= 1f)
        {
            ExitState_Move(GetNextState());
        }

        m_moveTimer += Time.deltaTime;
    }

    private void ExitState_Move(StateHandler targetState)
    {
        characterAnimator.SetBool("Move", false);
        m_stateHandler = targetState;
    }
    #endregion  // ~States

    private void SetMoveTarget(Vector3 position)
    {
        m_moveTimer = 0f;
        m_fromMovePos = transform.position;
        m_moveTarget = position;
    }

    public void StartAttackSequence(Character target, Vector3 pos, float attackTime)
    {
        m_attackSequence = new AttackSequence() { target = target, attackPosition = pos, attackTime = attackTime };
        m_attackTimer = attackTime;
        SetMoveTarget(pos);
        AddStateToQueue(StateType.Move);
        AddStateToQueue(StateType.Attack);
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
            GetComponent<Collider>().isTrigger = true;
            idlePoint.position = transform.position;
            m_idleRotation = transform.rotation;
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

    public void ResetPosition(Vector3 spawnPos)
    {
        transform.position = spawnPos;
        characterAnimator.SetFloat(m_idleAnimationHash, characterId);
        GetComponent<Collider>().isTrigger = false;
        m_rigidbody.isKinematic = false;
        m_rigidbody.useGravity = true;
        m_checkGrounded = true;
    }

    public void SpawnRagdoll(Vector3 forcePosition)
    {
        ragdoll.SetActive(true);
        meshObject.SetActive(false);
        CopyTransformTree(meshObject.transform, ragdoll.transform, forcePosition);
        m_ragdollSpawned = true;
    }

    public void DespawnRagdoll()
    {
        StartCoroutine(DespawnRagdollDelayed(1f));
    }

    private IEnumerator DespawnRagdollDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ragdoll.SetActive(false);
        meshObject.SetActive(true);
        m_ragdollSpawned = false;
        characterAnimator.SetFloat(m_idleAnimationHash, characterId);
    }

    private void CopyTransformTree(Transform sourceRoot, Transform destRoot, Vector3 forcePosition)
    {
        if (!m_ragdollSpawned)
        {
            sourceRoot.GetLocalPositionAndRotation(out var localPos, out var localRot);
            destRoot.SetLocalPositionAndRotation(localPos, localRot);
        }

        Rigidbody rb = destRoot.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddExplosionForce(Random.Range(3f, 7f), forcePosition - Vector3.up * Random.Range(0f, 0.5f), 1f);
        }

        int limit = Mathf.Min(sourceRoot.childCount, destRoot.childCount);

        for (int i = 0; i < limit; i++)
        {
            CopyTransformTree(sourceRoot.GetChild(i), destRoot.GetChild(i), forcePosition);
        }
    }

}
