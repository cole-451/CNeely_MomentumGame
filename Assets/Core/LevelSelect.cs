using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : Singleton<MenuManager>
{
    [SerializeField] public AudioSource musicPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartLvl1()
    {
        SceneManager.LoadScene("MainLevel1");
    }
    public void StartLvl2()
    {
        SceneManager.LoadScene("MainLevel2");
    }
    public void StartLvl3()
    {
        SceneManager.LoadScene("MainLevel3");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
