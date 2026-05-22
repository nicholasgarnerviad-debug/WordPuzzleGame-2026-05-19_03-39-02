using UnityEngine;

public class Init : MonoBehaviour
{
    private void Awake()
    {
        gameObject.AddComponent(System.Type.GetType("GameBootstrap"));
    }
}
