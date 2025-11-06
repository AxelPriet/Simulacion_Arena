using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 50;
    public int height = 30;
    public float updateTime = 0.1f;
    public GameObject cellPrefab;

    private bool[,] grid;
    private bool[,] nextGrid;
    private GameObject[,] cellObjects;
    private float timer;
    private bool isPaused = false;

    void Start()
    {
        grid = new bool[width, height];
        nextGrid = new bool[width, height];
        cellObjects = new GameObject[width, height];

        InputManager.Instance.OnPause += TogglePause;
        InputManager.Instance.OnRestart += RestartSimulation;
        InputManager.Instance.OnClear += ClearSimulation;
        InputManager.Instance.OnToggleCell += ToggleCellInput;

        GenerateGrid();
       // RandomizeGrid();  Para creacion de celulas alatorias
    }

    void Update()
    {
        if (isPaused) return;

        // Si se hace clic izquierdo: crear una célula (arena)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            HandleMouseClick(); // genera arena al hacer clic
        }

        // Actualización automática de la física granular
        timer += Time.deltaTime;
        if (timer >= updateTime)
        {
            Step();
            UpdateVisuals();
            timer = 0f;
        }
    }


    void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log(isPaused ? "Simulación pausada" : "Simulación reanudada");
    }

    void ToggleCellInput()
    {
        // Si hay mouse disponible (PC), usar clic real
        if (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
        {
            HandleMouseClick();
            return;
        }

        // Si no hay mouse, usar el centro de la cámara
        Vector3 camPos = Camera.main.transform.position;
        int x = Mathf.RoundToInt(camPos.x);
        int y = Mathf.RoundToInt(camPos.y);

        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        grid[x, y] = !grid[x, y];
        UpdateVisuals();
    }


    void ClearSimulation()
    {
        Debug.Log("Limpiando simulación...");
        ClearGrid();
        timer = 0f;
    }

    void RestartSimulation()
    {
        Debug.Log("Reiniciando simulación...");
        RandomizeGrid();
        timer = 0f;
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cell = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                cell.transform.parent = transform;
                cellObjects[x, y] = cell;
            }
        }
    }

    public void ClearGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = false;
            }
        }
        UpdateVisuals();
    }

    void RandomizeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = Random.value > 0.95f;
            }
        }
        UpdateVisuals();
    }

    void Step()
    {
        // Limpiar la siguiente grilla
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                nextGrid[x, y] = false;

        // 🔹 Recorremos de arriba hacia abajo (para evitar bucles visuales)
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y]) continue; // si no hay célula, saltar

                int newX = x;
                int newY = y;

                // Si puede caer directamente abajo
                if (y > 0 && !grid[x, y - 1])
                {
                    newY = y - 1;
                }
                else
                {
                    // Si abajo está ocupado, intenta moverse a los lados
                    bool canLeft = x > 0 && y > 0 && !grid[x - 1, y - 1];
                    bool canRight = x < width - 1 && y > 0 && !grid[x + 1, y - 1];

                    if (canLeft && canRight)
                    {
                        if (Random.value < 0.5f) newX = x - 1; else newX = x + 1;
                        newY = y - 1;
                    }
                    else if (canLeft)
                    {
                        newX = x - 1;
                        newY = y - 1;
                    }
                    else if (canRight)
                    {
                        newX = x + 1;
                        newY = y - 1;
                    }
                }

                // Mueve la célula a la nueva posición
                if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                    nextGrid[newX, newY] = true;
            }
        }

        // Intercambiar grillas
        var temp = grid;
        grid = nextGrid;
        nextGrid = temp;
    }



    int CountAliveNeighbors(int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (grid[nx, ny]) count++;
                }
            }
        }

        return count;
    }

    void HandleMouseClick()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);

        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        // Solo crea arena si no hay una célula ya existente
        if (!grid[x, y])
        {
            grid[x, y] = true;
        }
    }

    void UpdateVisuals()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var rend = cellObjects[x, y].GetComponent<SpriteRenderer>();
                rend.color = grid[x, y] ? Color.black : Color.white;
            }
        }
    }
}
