using UnityEditor;
using UnityEngine;

public class SetupCanvasCamera
{
    [MenuItem("Tools/Setup Game UI Canvas")]
    public static void SetupCanvas()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        var canvasComponent = canvas.GetComponent<Canvas>();
        if (canvasComponent == null)
        {
            Debug.LogError("Canvas component not found!");
            return;
        }

        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        var cameraComponent = mainCamera.GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("Camera component not found!");
            return;
        }

        canvasComponent.renderMode = RenderMode.ScreenSpaceCamera;
        canvasComponent.worldCamera = cameraComponent;

        Debug.Log("Canvas setup complete: renderMode=ScreenSpaceCamera, worldCamera assigned");
        EditorUtility.SetDirty(canvasComponent);
    }
}
