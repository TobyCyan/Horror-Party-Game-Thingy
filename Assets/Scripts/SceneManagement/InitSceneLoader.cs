using UnityEngine;

public class InitSceneLoader : MonoBehaviour
{
    [SerializeField] private string initScene = "NewMainMenu";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     	SceneLifetimeManager.Instance.clientSceneLoader.LoadSceneAsync(initScene); 
    }
}
