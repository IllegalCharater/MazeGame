using UnityEngine;

[DisallowMultipleComponent]
public sealed class MazeSceneController : MonoBehaviour
{
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private Transform player;
    [SerializeField] private MazeUIController mazeUI;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool beginRunOnStart = true;
    [SerializeField] private bool returnToShopWhenNoEnergyToStart = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        GameEvents.OnMazeRunEnded += OnMazeRunEnded;
    }

    private void Start()
    {
        StartMazeScene();
    }

    private void OnDisable()
    {
        GameEvents.OnMazeRunEnded -= OnMazeRunEnded;
    }

    public void StartMazeScene()
    {
        ResolveReferences();

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.InMaze);

        if (mazeGenerator != null)
        {
            if (!mazeGenerator.gameObject.activeSelf)
                mazeGenerator.gameObject.SetActive(true);
            if (generateOnStart)
                mazeGenerator.Generate();
            PlacePlayerAtStart();
        }

        if (beginRunOnStart && GameManager.Instance?.Services?.MazeRun != null)
        {
            bool started = GameManager.Instance.Services.MazeRun.BeginRun();
            if (!started)
            {
                if (mazeUI != null)
                    mazeUI.ShowMessage("Not enough energy to enter the maze.");
                if (returnToShopWhenNoEnergyToStart && SceneFlow.Instance != null)
                    SceneFlow.Instance.GoToShop();
                return;
            }
        }

        if (mazeUI != null)
            mazeUI.RefreshNow();
    }

    public void ReturnToShop()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToShop();
    }

    private void OnMazeRunEnded(MazeRunResult result)
    {
        if (mazeUI != null)
            mazeUI.ShowResult(result);
    }

    private void PlacePlayerAtStart()
    {
        if (player == null || mazeGenerator == null)
            return;

        Vector3 start = mazeGenerator.StartWorldPosition;
        player.position = new Vector3(start.x, start.y, player.position.z);

        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
            rb2d.velocity = Vector2.zero;
    }

    private void ResolveReferences()
    {
        if (mazeGenerator == null)
            mazeGenerator = FindObjectOfType<MazeGenerator>(true);
        if (mazeUI == null)
            mazeUI = FindObjectOfType<MazeUIController>(true);
        if (player == null)
        {
            GameObject found = GameObject.Find("Player");
            if (found != null)
                player = found.transform;
        }
    }
}
