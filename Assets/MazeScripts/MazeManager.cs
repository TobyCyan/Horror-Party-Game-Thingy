using UnityEngine;

/*
 * maze builder
 */
public class MazeManager : MonoBehaviour
{
    public static MazeManager Instance { get; private set; }
    public int size = 15;
    private float scale = 1f;
    int[] cells;
    MazeBlock[] mazeBlocks; // ref to all mazeblocks for now
    [SerializeField] private MazeBlock prefab;
    [SerializeField] private Trap[] booTrap; // remove later

    private void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        RegenerateAndRebuildMaze();
        AddTrap(booTrap[0], 0, 0);
        AddTrap(booTrap[1], 0, 0);
        AddTrap(booTrap[0], 0, 0);
        AddTrap(booTrap[1], 1, 3);
    }

    public void RegenerateAndRebuildMaze()
    {
        cells = MazeGenerator.Instance.GenerateMaze(size);
        BuildMaze();
    }

    public void BuildMaze()
    {
        mazeBlocks = new MazeBlock[size * size];
        // destroys existing maze before building new one?
        GameObject[] existingBlocks = GameObject.FindGameObjectsWithTag("MazeBlock");
        foreach (var block in existingBlocks)
        {
            Destroy(block);
        }

        float vert = 0;
        float hor = 0;

        for (int i = 0; i < cells.Length; i++)
        {
            if (i % size == 0)
            {
                vert += scale;
                hor = 0;
            }


            // instantiate mazeblock at hor 0 vert with state
            MazeBlock block = Instantiate(prefab, new Vector3(hor, 0, vert), Quaternion.identity);
            block.InitState(cells[i]);
            mazeBlocks[i] = block;
            // next
            hor += scale;
        }
    }

    public void AddTrap(Trap trap, int x, int z)
    {
        int idx(int x, int y) => x * size + y;
        mazeBlocks[idx(x, z)].RegisterTrap(trap);
    }
}
