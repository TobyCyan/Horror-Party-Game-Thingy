using UnityEngine;

public class InitSceneLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     	SceneLifetimeManager.Instance.clientSceneLoader.LoadSceneAsync("MainMenu"); 
    }
}
