using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class MazeGenerator : MonoBehaviour
{
    [Header("Size")]
    [SerializeField] private int width = 21;
    [SerializeField] private int height = 21;
    [SerializeField] private float cellSize = 2f;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject hazardPrefab;

    [Header("Auto Components")]
    [SerializeField] private bool ensureWallCollider2D = true;
    [SerializeField] private bool ensurePickupCollider2D = true;
    [SerializeField] private bool ensureExitTrigger = true;
    [SerializeField] private bool ensureHazardTrigger = true;

    [Header("Content")]
    [SerializeField] private int pickupCount = 8;
    [SerializeField] private int hazardCount = 6;
    [SerializeField] private int hazardEnergyCost = 2;
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnStart;
    [SerializeField] private bool autoGenerateInEditor;
    [SerializeField] private List<MazeLootEntry> lootTable = new List<MazeLootEntry>
    {
        new MazeLootEntry { itemId = "ingredient_carrot", minAmount = 1, maxAmount = 2, weight = 4 },
        new MazeLootEntry { itemId = "ingredient_meat", minAmount = 1, maxAmount = 1, weight = 2 },
        new MazeLootEntry { itemId = "ingredient_salt", minAmount = 1, maxAmount = 2, weight = 3 }
    };

    [Header("Hierarchy")]
    [SerializeField] private string generatedRootName = "GeneratedMaze";

    private readonly Vector2Int[] cardinalDirs =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    private readonly List<Vector2Int> walkableCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> occupiedContentCells = new HashSet<Vector2Int>();
    private Transform generatedRoot;
    private bool[,] walkable;
    private System.Random random;

    public Vector3 StartWorldPosition => GridToWorld(new Vector2Int(1, 1));

    [ContextMenu("Generate Maze")]
    public void Generate()
    {
        if (randomizeSeedOnStart)
            seed = Random.Range(int.MinValue, int.MaxValue);

        random = new System.Random(seed);
        PrepareGridData();
        BuildMazeByDfs();
        RebuildSceneObjects();
    }

#if UNITY_EDITOR
    [ContextMenu("Editor/Generate Random Static Maze")]
    private void GenerateRandomInEditor()
    {
        if (Application.isPlaying)
            return;

        seed = Random.Range(int.MinValue, int.MaxValue);
        Generate();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        width = Mathf.Max(7, width);
        height = Mathf.Max(7, height);
        cellSize = Mathf.Max(0.1f, cellSize);
        pickupCount = Mathf.Max(0, pickupCount);
        hazardCount = Mathf.Max(0, hazardCount);
        hazardEnergyCost = Mathf.Max(1, hazardEnergyCost);
        EnsureDefaultLootTable();

        if (Application.isPlaying || !autoGenerateInEditor)
            return;

        Generate();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void PrepareGridData()
    {
        width = Mathf.Max(7, width);
        height = Mathf.Max(7, height);
        if (width % 2 == 0)
            width += 1;
        if (height % 2 == 0)
            height += 1;
        cellSize = Mathf.Max(0.1f, cellSize);
        pickupCount = Mathf.Max(0, pickupCount);
        hazardCount = Mathf.Max(0, hazardCount);
        hazardEnergyCost = Mathf.Max(1, hazardEnergyCost);
        EnsureDefaultLootTable();

        walkable = new bool[width, height];
        walkableCells.Clear();
        occupiedContentCells.Clear();
    }

    private void BuildMazeByDfs()
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(1, 1);
        walkable[start.x, start.y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> nextCandidates = CollectUnvisitedTwoStepNeighbors(current);
            if (nextCandidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int next = nextCandidates[random.Next(nextCandidates.Count)];
            Vector2Int middle = new Vector2Int((current.x + next.x) / 2, (current.y + next.y) / 2);
            walkable[middle.x, middle.y] = true;
            walkable[next.x, next.y] = true;
            stack.Push(next);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (walkable[x, y])
                    walkableCells.Add(new Vector2Int(x, y));
            }
        }
    }

    private List<Vector2Int> CollectUnvisitedTwoStepNeighbors(Vector2Int cell)
    {
        List<Vector2Int> result = new List<Vector2Int>(4);
        for (int i = 0; i < cardinalDirs.Length; i++)
        {
            Vector2Int next = cell + cardinalDirs[i] * 2;
            if (next.x <= 0 || next.y <= 0 || next.x >= width - 1 || next.y >= height - 1)
                continue;
            if (walkable[next.x, next.y])
                continue;
            result.Add(next);
        }

        return result;
    }

    private void RebuildSceneObjects()
    {
        DestroyGeneratedRoot();

        GameObject root = new GameObject(generatedRootName);
        root.transform.SetParent(transform, false);
        generatedRoot = root.transform;

        Transform floorRoot = CreateChildRoot("Floor");
        Transform wallRoot = CreateChildRoot("Walls");
        Transform pickupRoot = CreateChildRoot("Pickups");
        Transform hazardRoot = CreateChildRoot("Hazards");
        Transform exitRoot = CreateChildRoot("Exit");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 world = GridToWorld(new Vector2Int(x, y));
                if (walkable[x, y])
                    SpawnFloor(floorRoot, world);
                else
                    SpawnWall(wallRoot, world);
            }
        }

        SpawnExit(exitRoot);
        SpawnPickups(pickupRoot);
        SpawnHazards(hazardRoot);
    }

    private void DestroyGeneratedRoot()
    {
        if (generatedRoot != null)
        {
            DestroyRuntimeObject(generatedRoot.gameObject);
            generatedRoot = null;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject existing = allObjects[i];
            if (existing != null && existing.name == generatedRootName)
                DestroyRuntimeObject(existing);
        }
    }

    private Transform CreateChildRoot(string name)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(generatedRoot, false);
        return node.transform;
    }

    private void SpawnFloor(Transform parent, Vector3 position)
    {
        if (floorPrefab != null)
        {
            GameObject obj = Instantiate(floorPrefab, position, Quaternion.identity, parent);
            obj.name = $"Floor_{position.x}_{position.y}";
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fallback.name = $"Floor_{position.x}_{position.y}";
        fallback.transform.SetParent(parent, false);
        fallback.transform.position = position + new Vector3(0f, 0f, 0.1f);
        fallback.transform.rotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one * cellSize;
        Collider col = fallback.GetComponent<Collider>();
        if (col != null)
            DestroyRuntimeObject(col);
    }

    private void SpawnWall(Transform parent, Vector3 position)
    {
        GameObject obj;
        if (wallPrefab != null)
        {
            obj = Instantiate(wallPrefab, position, Quaternion.identity, parent);
            obj.name = $"Wall_{position.x}_{position.y}";
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"Wall_{position.x}_{position.y}";
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
                DestroyRuntimeObject(col);
        }

        if (ensureWallCollider2D && obj.GetComponentInChildren<Collider2D>() == null)
        {
            BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(cellSize, cellSize);
        }
    }

    private void SpawnExit(Transform parent)
    {
        Vector2Int exitCell = FindFarthestWalkableFrom(new Vector2Int(1, 1));
        occupiedContentCells.Add(exitCell);
        Vector3 world = GridToWorld(exitCell);

        GameObject obj;
        if (exitPrefab != null)
            obj = Instantiate(exitPrefab, world, Quaternion.identity, parent);
        else
        {
            obj = new GameObject("MazeExit");
            obj.transform.SetParent(parent, false);
            obj.transform.position = world;
        }

        obj.name = "MazeExit";
        MazeExit mazeExit = obj.GetComponent<MazeExit>();
        if (mazeExit == null)
            mazeExit = obj.AddComponent<MazeExit>();

        if (ensureExitTrigger)
        {
            CircleCollider2D trigger2D = obj.GetComponent<CircleCollider2D>();
            if (trigger2D == null)
                trigger2D = obj.AddComponent<CircleCollider2D>();
            trigger2D.isTrigger = true;
            trigger2D.radius = Mathf.Max(0.5f, cellSize * 0.35f);
        }
    }

    private void SpawnPickups(Transform parent)
    {
        if (pickupPrefab == null || pickupCount <= 0 || walkableCells.Count <= 2)
            return;

        int count = Mathf.Min(pickupCount, walkableCells.Count - 2);
        int guard = 0;

        while (count > 0 && guard < 10000)
        {
            guard++;
            if (!TryReserveRandomContentCell(out Vector2Int cell))
                break;

            MazeLootEntry loot = RollLootEntry();
            if (loot == null || string.IsNullOrEmpty(loot.itemId))
                continue;

            Vector3 pos = GridToWorld(cell);
            GameObject obj = Instantiate(pickupPrefab, pos, Quaternion.identity, parent);
            obj.name = $"Pickup_{loot.itemId}_{cell.x}_{cell.y}";

            Pickup pickup = obj.GetComponent<Pickup>();
            if (pickup == null)
                pickup = obj.AddComponent<Pickup>();
            pickup.Configure(loot.itemId, loot.RollAmount(random), PickupType.MazeRunLoot);

            if (ensurePickupCollider2D && obj.GetComponentInChildren<Collider2D>() == null)
            {
                CircleCollider2D c2d = obj.AddComponent<CircleCollider2D>();
                c2d.isTrigger = true;
                c2d.radius = Mathf.Max(0.2f, cellSize * 0.25f);
            }

            count--;
        }
    }

    private void SpawnHazards(Transform parent)
    {
        if (hazardCount <= 0 || walkableCells.Count <= 2)
            return;

        int count = Mathf.Min(hazardCount, walkableCells.Count - 2 - occupiedContentCells.Count);
        int guard = 0;

        while (count > 0 && guard < 10000)
        {
            guard++;
            if (!TryReserveRandomContentCell(out Vector2Int cell))
                break;

            Vector3 pos = GridToWorld(cell);
            GameObject obj;
            if (hazardPrefab != null)
                obj = Instantiate(hazardPrefab, pos, Quaternion.identity, parent);
            else
                obj = CreateFallbackHazard(pos, parent);

            obj.name = $"Hazard_{cell.x}_{cell.y}";
            MazeHazard hazard = obj.GetComponent<MazeHazard>();
            if (hazard == null)
                hazard = obj.AddComponent<MazeHazard>();
            hazard.Configure(hazardEnergyCost, true);

            if (ensureHazardTrigger && obj.GetComponentInChildren<Collider2D>() == null)
            {
                CircleCollider2D c2d = obj.AddComponent<CircleCollider2D>();
                c2d.isTrigger = true;
                c2d.radius = Mathf.Max(0.25f, cellSize * 0.3f);
            }

            count--;
        }
    }

    private GameObject CreateFallbackHazard(Vector3 position, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position + new Vector3(0f, 0f, -0.05f);
        obj.transform.localScale = Vector3.one * (cellSize * 0.45f);

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            DestroyImmediate(col);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = new Color(0.9f, 0.15f, 0.1f, 0.9f);
            renderer.sharedMaterial = material;
        }

        return obj;
    }

    private bool TryReserveRandomContentCell(out Vector2Int cell)
    {
        cell = default;
        if (walkableCells.Count == 0)
            return false;

        for (int i = 0; i < 100; i++)
        {
            Vector2Int candidate = walkableCells[random.Next(walkableCells.Count)];
            if (IsReservedContentCell(candidate))
                continue;

            occupiedContentCells.Add(candidate);
            cell = candidate;
            return true;
        }

        for (int i = 0; i < walkableCells.Count; i++)
        {
            Vector2Int candidate = walkableCells[i];
            if (IsReservedContentCell(candidate))
                continue;

            occupiedContentCells.Add(candidate);
            cell = candidate;
            return true;
        }

        return false;
    }

    private bool IsReservedContentCell(Vector2Int cell)
    {
        if (cell.x == 1 && cell.y == 1)
            return true;
        return occupiedContentCells.Contains(cell);
    }

    private Vector2Int FindFarthestWalkableFrom(Vector2Int start)
    {
        Vector2Int best = start;
        int bestDist = -1;

        for (int i = 0; i < walkableCells.Count; i++)
        {
            Vector2Int cell = walkableCells[i];
            int dist = Mathf.Abs(cell.x - start.x) + Mathf.Abs(cell.y - start.y);
            if (dist > bestDist)
            {
                bestDist = dist;
                best = cell;
            }
        }

        return best;
    }

    private MazeLootEntry RollLootEntry()
    {
        EnsureDefaultLootTable();

        int totalWeight = 0;
        for (int i = 0; i < lootTable.Count; i++)
        {
            MazeLootEntry entry = lootTable[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.weight <= 0)
                continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = random.Next(1, totalWeight + 1);
        int cursor = 0;
        for (int i = 0; i < lootTable.Count; i++)
        {
            MazeLootEntry entry = lootTable[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.weight <= 0)
                continue;
            cursor += entry.weight;
            if (roll <= cursor)
                return entry;
        }

        return lootTable[0];
    }

    private void EnsureDefaultLootTable()
    {
        if (lootTable != null && lootTable.Count > 0)
            return;

        lootTable = new List<MazeLootEntry>
        {
            new MazeLootEntry { itemId = "ingredient_carrot", minAmount = 1, maxAmount = 2, weight = 4 },
            new MazeLootEntry { itemId = "ingredient_meat", minAmount = 1, maxAmount = 1, weight = 2 },
            new MazeLootEntry { itemId = "ingredient_salt", minAmount = 1, maxAmount = 2, weight = 3 }
        };
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    private void DestroyRuntimeObject(Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
