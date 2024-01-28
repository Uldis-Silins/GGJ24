using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelController : MonoBehaviour
{
    [System.Serializable]
    public class Cell
    {
        public Room room;

        public Cell(Room room)
        {
            this.room = room;
        }
    }

    public Camera mainCamera;
    public Room roomPrefab;
    public Transform roomParent;
    public List<Room> rooms;

    public float margin = 0.2f;

    public TextMeshProUGUI winLabel;
    public TextMeshProUGUI scoreLabel;

    [SerializeField] private Room m_selectedRoom;
    [SerializeField] private Bounds m_homeBounds;

    private bool m_isWin;

    private Vector3 m_startDragPosition;

    private Cell[,] m_cells;

    private Vector3 m_parentOffset = Vector3.one * 0.5f;

    private void OnValidate()
    {
        if (rooms != null && rooms.Count > 0)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].id = i;
            }
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Start()
    {
        mainCamera.orthographic = true;

        if(rooms != null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Destroy(rooms[i].gameObject);
            }
        }

        rooms.Clear();

        m_cells = new Cell[3,3];

        SpawnRooms();

        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                int index = y * m_cells.GetLength(0) + x;

                m_cells[x, y] = new Cell(rooms[index]);

                Room room = m_cells[x, y].room;

                room.ToggleInteractable(!room.IsEmpty);
                
                rooms[index].transform.position = new Vector3(x, y) + new Vector3(margin * x, margin * y) + m_parentOffset;
            }
        }

        GetBounds();

        if (m_homeBounds != null)
        {
            FocusOn(mainCamera, m_parentOffset, m_homeBounds, margin);
        }

        StartCoroutine(RandomizeRooms());
    }

    private void Update()
    {
        if (m_isWin) return;

        int t = (int)(600 - (Time.time * 3));
        string timeText = t % 7 == 0 ? t.ToString("X") : System.Convert.ToString(t, 2);
        scoreLabel.text = timeText;

        if (Input.GetMouseButtonDown(0))
        {
            if (!CanMove()) return;

            Room hitRoom = CheckRoomHit();

            if (hitRoom != null && hitRoom.CanMove)
            {
                m_selectedRoom = hitRoom;
                m_startDragPosition = Input.mousePosition;
            }
        }
        else if(Input.GetMouseButton(0))
        {
            if(m_selectedRoom != null)
            {
                Vector3 dir = (Input.mousePosition - m_startDragPosition).normalized;

                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 targetPos = m_selectedRoom.transform.position;
                mouseWorldPos.z = m_selectedRoom.transform.position.z;

                Vector2Int roomIndex = new Vector2Int();
                if (GetRoomIndex(m_selectedRoom, ref roomIndex))
                {
                    Vector2Int targetIndex = roomIndex + new Vector2Int(Mathf.RoundToInt(dir.x), 0);

                    targetIndex.Clamp(Vector2Int.zero, new Vector2Int(m_cells.GetLength(0) - 1, m_cells.GetLength(1) - 1));

                    if (m_cells[targetIndex.x, targetIndex.y].room.IsEmpty)
                    {
                        targetPos.x = mouseWorldPos.x;
                        targetPos.y = m_selectedRoom.transform.position.y;
                    }

                    targetIndex = roomIndex + new Vector2Int(0, Mathf.RoundToInt(dir.y));

                    targetIndex.Clamp(Vector2Int.zero, new Vector2Int(m_cells.GetLength(0) - 1, m_cells.GetLength(1) - 1));

                    if (m_cells[targetIndex.x, targetIndex.y].room.IsEmpty)
                    {
                        targetPos.y = mouseWorldPos.y;
                        targetPos.x = m_selectedRoom.transform.position.x;
                    }
                }

                m_selectedRoom.transform.position = targetPos;
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            if(m_selectedRoom != null)
            {
                Vector3 dir = (Input.mousePosition - m_startDragPosition);

                Vector2Int checkDirection = new Vector2Int();

                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    checkDirection.x = dir.x > 0 ? 1 : -1;
                }
                else
                {
                    checkDirection.y = dir.y > 0 ? 1 : -1;
                }

                Vector2Int roomIndex = new Vector2Int();
                if (GetRoomIndex(m_selectedRoom, ref roomIndex))
                {
                    Vector2Int targetIndex = roomIndex + checkDirection;

                    targetIndex.Clamp(Vector2Int.zero, new Vector2Int(m_cells.GetLength(0) - 1, m_cells.GetLength(1) - 1));

                    if (m_cells[targetIndex.x, targetIndex.y].room.IsEmpty)
                    {
                        Swap(roomIndex, targetIndex, true);
                    }
                    else
                    {
                        m_cells[roomIndex.x, roomIndex.y].room.SetTargetPosition(GetCellPosition(roomIndex.x, roomIndex.y));
                    }

                    CheckNeighbors(m_selectedRoom);

                    if (CheckWin())
                    {
                        //winLabel.gameObject.SetActive(true);

                        m_isWin = true;

                        foreach (var room in rooms)
                        {
                            if (!room.IsEmpty)
                            {
                                room.character.SetWin();
                            }
                        }

                        StartCoroutine(ResetWinState());
                    }
                }

                m_selectedRoom = null;
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            winLabel.gameObject.SetActive(false);
            Swap(new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), true);

            if(CheckWin())
            {
                //winLabel.gameObject.SetActive(true);

                m_isWin = true;

                foreach (var room in rooms)
                {
                    if(!room.IsEmpty)
                    {
                        room.character.Unfreeze();
                        room.character.SetWin();
                    }
                }

                StartCoroutine(ResetWinState());
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if(m_homeBounds != null)
        {
            Gizmos.DrawWireCube(m_homeBounds.center, m_homeBounds.size);
        }
    }

    private Bounds GetBounds()
    {
        if (rooms != null && rooms.Count > 0)
        {
            Bounds total = new Bounds();

            foreach (var room in rooms)
            {
                Renderer r = room.GetComponent<Renderer>();

                if (r != null)
                {
                    total.Encapsulate(r.bounds);
                }
            }

            return total;
        }

        throw new System.Exception("No rooms, cannot calculate bounds");
    }

    public void FocusOn(Camera camera, Vector3 offset, Bounds bounds, float margin)
    {
        float w = bounds.size.x + margin;
        float h = bounds.size.y + margin;
        //float x = offset.x + w * 0.5f - margin * 3;
        //float y = offset.y + h * 0.5f - margin * 3;

        //camera.transform.position = new Vector3(x, y, -10f);
        camera.transform.position += Vector3.right * (offset.x + w * 0.5f - margin * 3);

        camera.orthographicSize = ((w > h * camera.aspect) ? (float)w / (float)camera.pixelWidth * camera.pixelHeight : h) / 2;
        camera.orthographicSize += margin;
    }

    private void Swap(Vector2Int i1, Vector2Int i2, bool animate)
    {
        if (animate)
        {
            m_cells[i1.x, i1.y].room.SetTargetPosition(GetCellPosition(i2.x, i2.y));
            m_cells[i2.x, i2.y].room.SetTargetPosition(GetCellPosition(i1.x, i1.y));
        }
        else
        {
            Vector3 pos = m_cells[i1.x, i1.y].room.transform.position;
            m_cells[i1.x, i1.y].room.transform.position = m_cells[i2.x, i2.y].room.transform.position;
            m_cells[i2.x, i2.y].room.transform.position = pos;
        }

        Cell temp = m_cells[i1.x, i1.y];
        m_cells[i1.x, i1.y] = m_cells[i2.x, i2.y];
        m_cells[i2.x, i2.y] = temp;
    }

    private void SpawnRooms()
    {
        for (int i = 0; i < m_cells.GetLength(0) * m_cells.GetLength(1); i++)
        {
            Room instance = Instantiate(roomPrefab, roomParent);
            instance.name = "Room " + i;
            rooms.Add(instance);
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            if(i == rooms.Count - 1)
            {
                rooms[i].id = i;
                break;
            }

            rooms[i].id = i;
            rooms[i].SpawnCharacter();
            rooms[i].character.characterId = i;
        }
    }

    private void CheckNeighbors(Room room)
    {
        Vector2Int roomIndex = new Vector2Int();

        foreach (var r in rooms)
        {
            if (!r.IsEmpty && r != room)
            {
                r.character.Freeze();
            }
        }

        if(GetRoomIndex(room, ref roomIndex))
        {
            if (m_cells[roomIndex.x, roomIndex.y].room.IsEmpty) Debug.LogError("Don't check the empty room pls");

            if(roomIndex.x > 0)
            {
                Cell leftNeighbor = m_cells[roomIndex.x - 1, roomIndex.y];

                if (!leftNeighbor.room.IsEmpty)
                {
                    if (leftNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                    {
                        Debug.Log("Left is bad");
                        leftNeighbor.room.PlayAttackSequence(m_selectedRoom);
                        leftNeighbor.room.character.Unfreeze();
                    }
                    else
                    {
                        leftNeighbor.room.PlayDanceSequence();
                        leftNeighbor.room.character.Unfreeze();
                    }
                }
            }

            if(roomIndex.x < m_cells.GetLength(1) - 1)
            {
                Cell rightNeighbor = m_cells[roomIndex.x + 1, roomIndex.y];

                if (!rightNeighbor.room.IsEmpty)
                {
                    if (rightNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                    {
                        Debug.Log("Right is bad");
                        rightNeighbor.room.PlayAttackSequence(m_selectedRoom);
                        rightNeighbor.room.character.Unfreeze();
                    }
                    else
                    {
                        rightNeighbor.room.PlayDanceSequence();
                        rightNeighbor.room.character.Unfreeze();
                    }
                }
            }

            if(roomIndex.y > 0)
            {
                Cell downNeighbor = m_cells[roomIndex.x, roomIndex.y - 1];

                if (!downNeighbor.room.IsEmpty)
                {
                    if (downNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                    {
                        Debug.Log("Down is bad");
                        downNeighbor.room.PlayAttackSequence(m_selectedRoom);
                        downNeighbor.room.character.Unfreeze();
                    }
                    else
                    {
                        downNeighbor.room.PlayDanceSequence();
                        downNeighbor.room.character.Unfreeze();
                    }
                }
            }

            if(roomIndex.y < m_cells.GetLength(0) - 1)
            {
                Cell upNeighbor = m_cells[roomIndex.x, roomIndex.y + 1];

                if (!upNeighbor.room.IsEmpty)
                {
                    if (upNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                    {
                        Debug.Log("Up is bad");
                        upNeighbor.room.PlayAttackSequence(m_selectedRoom);
                        upNeighbor.room.character.Unfreeze();
                    }
                    else
                    {
                        upNeighbor.room.PlayDanceSequence();
                        upNeighbor.room.character.Unfreeze();
                    }
                }
            }

            StartCoroutine(UnfreezeChars());
        }
    }

    private IEnumerator UnfreezeChars()
    {
        yield return new WaitForSeconds(3f);
        foreach (var r in rooms)
        {
            if (!r.IsEmpty)
            {
                r.character.Unfreeze();
            }
        }
    }

    private bool CheckWin()
    {
        bool isWin = true;

        //for (int y = 0; y < m_cells.GetLength(0); y++)
        //{
        //    for (int x = 0; x < m_cells.GetLength(1); x++)
        //    {
        //        m_cells[x, y].room.roomRenderer.material.color = Color.gray;
        //    }
        //}

        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                if (m_cells[x, y].room.IsEmpty) continue;

                if (x > 0)
                {
                    Cell leftNeighbor = m_cells[x - 1, y];

                    if (!leftNeighbor.room.IsEmpty && leftNeighbor.room.character.type == m_cells[x, y].room.character.type)
                    {
                        //leftNeighbor.room.roomRenderer.material.color = Color.red;
                        //m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if (x < m_cells.GetLength(1) - 1)
                {
                    Cell rightNeighbor = m_cells[x + 1, y];

                    if (!rightNeighbor.room.IsEmpty && rightNeighbor.room.character.type == m_cells[x, y].room.character.type)
                    {
                        //rightNeighbor.room.roomRenderer.material.color = Color.red;
                        //m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if (y > 0)
                {
                    Cell downNeighbor = m_cells[x, y - 1];

                    if(!downNeighbor.room.IsEmpty && downNeighbor.room.character.type == m_cells[x,y].room.character.type)
                    {
                        //upNeighbor.room.roomRenderer.material.color = Color.red;
                        //m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if(y < m_cells.GetLength(0) - 1)
                {
                    Cell upNeighbor = m_cells[x, y + 1];

                    if(!upNeighbor.room.IsEmpty && upNeighbor.room.character.type == m_cells[x,y].room.character.type)
                    {
                        //downNeighbor.room.roomRenderer.material.color = Color.red;
                        //m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }
            }
        }

        return isWin;
    }

    private bool CanMove()
    {
        foreach (var room in rooms)
        {
            if(room.IsLocked)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator RandomizeRooms()
    {
        const int randomIterations = 5;
        for (int i = 0; i < randomIterations; i++)
        {
            Swap(new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), false);
            yield return null;
        }

        CheckWin();

        foreach (var room in rooms)
        {
            if (!room.IsEmpty)
            {
                room.character.ResetPosition(room.characterSpawnLocation.position);
            }
        }
    }

    private Room CheckRoomHit()
    {
        RaycastHit hit;
        if(Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 100f, 1 << LayerMask.NameToLayer("Room")))
        {
            return hit.transform.GetComponent<Room>();
        }

        return null;
    }

    private bool IsInsideBounds(Vector3 pos)
    {
        Vector3 margins = new Vector3(margin * (m_cells.GetLength(0)), margin * (m_cells.GetLength(1)));
        Vector3 size = new Vector3(m_cells.GetLength(0) - 1, m_cells.GetLength(1) - 1) + margins;

        Rect rect = new Rect(Vector3.zero, size);

        return rect.Contains(pos);
    }

    private Vector2Int GetCellIndex(Vector3 pos)
    {
        Vector3 margins = new Vector3(margin * (m_cells.GetLength(0)), margin * (m_cells.GetLength(1)));
        Vector3 size = new Vector3(m_cells.GetLength(0), m_cells.GetLength(1));
        
        return new Vector2Int(Mathf.FloorToInt((pos.x / size.x) * size.x), Mathf.FloorToInt((pos.y / size.y) * size.y));
    }

    private bool GetRoomIndex(Room room, ref Vector2Int index)
    {
        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                if (m_cells[x,y].room.id == room.id)
                {
                    index = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 GetCellPosition(int x, int y)
    {
        return new Vector3(x, y) + new Vector3(margin * x, margin * y) + m_parentOffset;
    }

    private IEnumerator ResetWinState()
    {
        const float resetWinDelay = 5f;
        yield return new WaitForSeconds(resetWinDelay);
        m_isWin = false;

        foreach (var room in rooms)
        {
            if (!room.IsEmpty)
            {
                room.character.ResetWin();
            }
        }

        StartCoroutine(RandomizeRooms());
    }

    public void OnResetClick()
    {
        StartCoroutine(RandomizeRooms());
    }

    public void OnSettingsClick()
    {
        foreach (var room in rooms)
        {
            if(!room.IsEmpty)
            {
                room.character.SpawnRagdoll(room.character.transform.position - Vector3.up * 0.25f, 5f);
            }
        }

        StartCoroutine(ResetChharacters());
    }

    private IEnumerator ResetChharacters()
    {
        yield return new WaitForSeconds(2f);

        foreach (var room in rooms)
        {
            if (!room.IsEmpty)
            {
                room.character.DespawnRagdoll();
            }
        }
    }
}
