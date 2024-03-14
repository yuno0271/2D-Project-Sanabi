using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    #region �̱��� �޼���
    public static LayerManager Layer { get { return LayerManager.Instance; }} 
    public static PoolManager Pool { get { return PoolManager.Instance; } }
    public static GameManager Game { get { return GameManager.Instance; } }
    public static CameraManager Camera { get { return CameraManager.Instance; }}

    // ���� �ε�Ǳ� ���� �̱��� ����
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameManager.ReleaseInstance();
        LayerManager.ReleaseInstance();
        PoolManager.ReleaseInstance();
        CameraManager.ReleaseInstance();

        GameManager.CreateInstance();
        LayerManager.CreateInstance();
        PoolManager.CreateInstance();
        CameraManager.CreateInstance();
    }
    #endregion
}
