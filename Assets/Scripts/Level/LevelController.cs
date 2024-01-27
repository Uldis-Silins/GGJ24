using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public TMPro.TextMeshProUGUI winLabel;

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

        if (Input.GetMouseButtonDown(0))
        {
            Room hitRoom = CheckRoomHit();

            if (hitRoom != null)
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
                Debug.Log(Mathf.RoundToInt(dir.x) + ": " + Mathf.RoundToInt(dir.y));

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

                Debug.Log(checkDirection);

                Vector2Int roomIndex = new Vector2Int();
                if (GetRoomIndex(m_selectedRoom, ref roomIndex))
                {
                    Vector2Int targetIndex = roomIndex + checkDirection;
                    Debug.Log("Swapping " + roomIndex + " with " + targetIndex);

                    targetIndex.Clamp(Vector2Int.zero, new Vector2Int(m_cells.GetLength(0) - 1, m_cells.GetLength(1) - 1));

                    if (m_cells[targetIndex.x, targetIndex.y].room.IsEmpty)
                    {
                        Swap(roomIndex, targetIndex, true);
                    }
                    else
                    {
                        m_cells[roomIndex.x, roomIndex.y].room.SetTargetPosition(GetCellPosition(roomIndex.x, roomIndex.y));
                    }

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

                        CheckNeighbors(m_selectedRoom);

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
        float x = offset.x + w * 0.5f - margin * 3;
        float y = offset.y + h * 0.5f - margin * 3;

        camera.transform.position = new Vector3(x, y, -10f);

        camera.orthographicSize = ((w > h * camera.aspect) ? (float)w / (float)camera.pixelWidth * camera.pixelHeight : h) / 2;
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

        if(GetRoomIndex(room, ref roomIndex))
        {
            if (m_cells[roomIndex.x, roomIndex.y].room.IsEmpty) Debug.LogError("Don't check the empty room pls");

            if(roomIndex.x > 0)
            {
                Cell leftNeighbor = m_cells[roomIndex.x - 1, roomIndex.y];

                if (!leftNeighbor.room.IsEmpty && leftNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                {
                    Debug.Log("Left is bad");
                }
            }

            if(roomIndex.x < m_cells.GetLength(1) - 1)
            {
                Cell rightNeighbor = m_cells[roomIndex.x + 1, roomIndex.y];

                if (!rightNeighbor.room.IsEmpty && rightNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                {
                    Debug.Log("Right is bad");
                }
            }

            if(roomIndex.y > 0)
            {
                Cell upNeighbor = m_cells[roomIndex.x, roomIndex.y - 1];

                if (!upNeighbor.room.IsEmpty && upNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                {
                    Debug.Log("Up is bad");
                }
            }

            if(roomIndex.y < m_cells.GetLength(0) - 1)
            {
                Cell downNeighbor = m_cells[roomIndex.x, roomIndex.y + 1];

                if (!downNeighbor.room.IsEmpty && downNeighbor.room.character.type == m_cells[roomIndex.x, roomIndex.y].room.character.type)
                {
                    Debug.Log("Down is bad");
                }
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
                    Cell upNeighbor = m_cells[x, y - 1];

                    if(!upNeighbor.room.IsEmpty && upNeighbor.room.character.type == m_cells[x,y].room.character.type)
                    {
                        //upNeighbor.room.roomRenderer.material.color = Color.red;
                        //m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if(y < m_cells.GetLength(0) - 1)
                {
                    Cell downNeighbor = m_cells[x, y + 1];

                    if(!downNeighbor.room.IsEmpty && downNeighbor.room.character.type == m_cells[x,y].room.character.type)
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

    private IEnumerator RandomizeRooms()
    {
        const int randomIterations = 5;
        for (int i = 0; i < randomIterations; i++)
        {
            Swap(new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), false);
            yield return null;
        }

        CheckWin();
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
}
