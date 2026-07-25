using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillBarrier : MonoBehaviour
{
    [SerializeField] public string CurrentScene;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SceneManager.LoadScene(CurrentScene);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
