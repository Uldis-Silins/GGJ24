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
    public List<Room> rooms;

    public float margin = 0.2f;

    public TMPro.TextMeshProUGUI winLabel;

    [SerializeField] private Room m_selectedRoom;
    [SerializeField] private Bounds m_homeBounds;

    private Cell[,] m_cells;

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

        Vector3 parentOffset = Vector3.one * 0.5f;
        m_cells = new Cell[3,3];

        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                int index = y * m_cells.GetLength(0) + x;

                m_cells[x, y] = new Cell(rooms[index]);

                Room room = m_cells[x, y].room;
                //Color c = room.type == Room.RoomType.Circle ? Color.red : Color.white;

                room.ToggleInteractable(room.type != Room.RoomType.None);

                //rooms[index].GetComponent<Renderer>().material.color = c;

                rooms[index].transform.position = new Vector3(x, y) + new Vector3(margin * x, margin * y) + parentOffset;
            }
        }

        GetBounds();

        if (m_homeBounds != null)
        {
            FocusOn(mainCamera, parentOffset, m_homeBounds, margin);
        }

        StartCoroutine(RandomizeRooms());
        //CheckNeighbors();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Room hitRoom = CheckRoomHit();

            if (hitRoom != null)
            {
                m_selectedRoom = hitRoom;
            }
        }
        else if(Input.GetMouseButton(0))
        {
            if(m_selectedRoom != null)
            {
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 targetPos = mouseWorldPos;
                targetPos.z = m_selectedRoom.transform.position.z;
                m_selectedRoom.transform.position = targetPos;
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            if(m_selectedRoom != null)
            {
                m_selectedRoom = null;
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            winLabel.gameObject.SetActive(false);
            Swap(new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), new Vector2Int(Random.Range(0, 3), Random.Range(0, 3)), true);
            if(CheckNeighbors())
            {
                winLabel.gameObject.SetActive(true);
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

        Gizmos.color = Color.yellow;
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
            m_cells[i1.x, i1.y].room.SetTargetPosition(m_cells[i2.x, i2.y].room.transform.position);
            m_cells[i2.x, i2.y].room.SetTargetPosition(m_cells[i1.x, i1.y].room.transform.position);
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

    private bool CheckNeighbors()
    {
        bool isWin = true;

        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                m_cells[x, y].room.roomRenderer.material.color = Color.white;
            }
        }

        for (int y = 0; y < m_cells.GetLength(0); y++)
        {
            for (int x = 0; x < m_cells.GetLength(1); x++)
            {
                if (m_cells[x, y].room.type == Room.RoomType.None) continue;

                if (x > 0)
                {
                    Cell leftNeighbor = m_cells[x - 1, y];

                    if (leftNeighbor.room.type == m_cells[x, y].room.type)
                    {
                        leftNeighbor.room.roomRenderer.material.color = Color.red;
                        m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if (x < m_cells.GetLength(1) - 1)
                {
                    Cell rightNeighbor = m_cells[x + 1, y];

                    if (rightNeighbor.room.type == m_cells[x, y].room.type)
                    {
                        rightNeighbor.room.roomRenderer.material.color = Color.red;
                        m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if (y > 0)
                {
                    Cell upNeighbor = m_cells[x, y - 1];

                    if(upNeighbor.room.type == m_cells[x,y].room.type)
                    {
                        upNeighbor.room.roomRenderer.material.color = Color.red;
                        m_cells[x, y].room.roomRenderer.material.color = Color.red;
                        isWin = false;
                    }
                }

                if(y < m_cells.GetLength(0) - 1)
                {
                    Cell downNeighbor = m_cells[x, y + 1];

                    if(downNeighbor.room.type == m_cells[x,y].room.type)
                    {
                        downNeighbor.room.roomRenderer.material.color = Color.red;
                        m_cells[x, y].room.roomRenderer.material.color = Color.red;
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

        CheckNeighbors();
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
}
