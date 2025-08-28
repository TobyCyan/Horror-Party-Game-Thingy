using UnityEngine;

public class Builder : MonoBehaviour
{
    public static Builder Instance { get; private set; }

    private float scale = 1f;
    [SerializeField] private MazeBlock prefab;

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
    public void BuildMaze(int[] cells, int size)
    {
        float startX = -size * scale / 2f;
        float startZ = -size * scale / 2f;

        float vert = startZ;
        float hor = startX;

        for (int i = 0; i < cells.Length; i++)
        {
            if (i % size == 0)
            {
                vert += scale;
                hor = startX;
            }


            // instantiate mazeblock at hor 0 vert with state
            MazeBlock block = GameObject.Instantiate(prefab, new Vector3(hor, 0, vert), Quaternion.identity);
            block.InitState(cells[i]);
            // next
            hor += scale;
        }
    }
}
