using UnityEngine;
using Unity.Netcode;

/*
 * maze builder
 */
public class MazeManager : NetworkBehaviour
{
    public static MazeManager Instance { get; private set; }
    public int size = 15;
    public float roomRate = 0.2f;
    private float scale = 6f; // should be same as prefab scale, SORRY
    int[] cells;
    MazeBlock[] mazeBlocks; // ref to all mazeblocks for now
    [SerializeField] private MazeBlock prefab;
    [SerializeField] private MazeBlock roomPrefab;

    private NetworkVariable<int> mazeSeed = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // how does this work in multiplayer
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int seed = Random.Range(int.MinValue, int.MaxValue);
            mazeSeed.Value = seed;
        }


        mazeSeed.OnValueChanged += OnSeedChanged;
        if (mazeSeed.Value != 0)
        {
            BuildMazeWithSeed(mazeSeed.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        mazeSeed.OnValueChanged -= OnSeedChanged;
    }

    private void OnSeedChanged(int oldSeed, int newSeed)
    {
        BuildMazeWithSeed(newSeed);
    }
    

    public void BuildMazeWithSeed(int seed)
    {

        DestroyMaze();
        MazeGenerator generator = new MazeGenerator(size, seed, roomRate);
        cells = generator.GenerateMaze();
        BuildMaze();
    }

    private void BuildMaze()
    {

        float vert = 0;
        float hor = 0;

        for (int i = 0; i < cells.Length; i++)
        {
            if (i % size == 0)
            {
                vert += scale;
                hor = 0;
            }

            MazeBlock blockPrefab = prefab;

            if (CellUtils.IsRoom(cells[i])) blockPrefab = roomPrefab;

            // instantiate mazeblock at hor 0 vert with state
            MazeBlock block = Instantiate(blockPrefab, new Vector3(hor, 0, vert), Quaternion.identity);
            block.InitState(cells[i]);
            mazeBlocks[i] = block;
            // next
            hor += scale;
        }
    }

    private void DestroyMaze()
    {
        // if this doesn't work maybe switch back to prev version of cleanup
        if (mazeBlocks != null)
        {
            foreach (var block in mazeBlocks)
            {
                if (block != null) Destroy(block.gameObject);
            }
        }
        mazeBlocks = new MazeBlock[size * size];

        /*
        GameObject[] existingBlocks = GameObject.FindGameObjectsWithTag("MazeBlock");
        foreach (var block in existingBlocks)
        {
            Destroy(block);
        }
        */
    }

}
