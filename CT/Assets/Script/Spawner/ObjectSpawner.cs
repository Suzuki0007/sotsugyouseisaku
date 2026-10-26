/*********************************************************************/
// * \file   ObjectSpawner.cs
// * \brief  オブジェクトスポナークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// オブジェクトスポナークラス
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("スポナー設定")]

    [Tooltip("スポーンするオブジェクト")]
    [SerializeField]
    private GameObject spawnObject;

    [Tooltip("スポーンするオブジェクトの位置")]
    [SerializeField]
    private Transform spawnPosition;

    [Tooltip("スポーン間隔")]
    [SerializeField]
    private float spawnInterval = 10f;

    [Tooltip("無限にスポーンするかどうか")]
    [SerializeField]
    private bool isLoopSpawn = true;

    [Tooltip("最大スポーン数")]
    [SerializeField]
    private int maxSpawnCount = 15;

    private GameObject currentObject;

    private int spawnCount = 0;

    private bool isRespawning = false;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(spawnObject == null) { Debug.LogError("スポーンするオブジェクトが設定されていません"); }
        if(spawnPosition == null) { Debug.LogError("スポーン位置が設定されていません"); }
        if(spawnInterval <= 0f) { Debug.LogError("スポーン間隔が0以下です"); }

        if(!isLoopSpawn && maxSpawnCount <= 0) { Debug.LogWarning("最大生成数が0以下のため、スポーンしません", this); }
    }

    private void Start()
    {
        // 初回スポーン
        SpawnObject();
    }

    private void SpawnObject()
    {
        if(currentObject != null) { return; }

        if(!isLoopSpawn && spawnCount >= maxSpawnCount)
        {
            Debug.Log("最大生成数に達したためスポーンを停止します", this);
            return;
        }

        // オブジェクトを生成する
        currentObject = Instantiate(spawnObject, spawnPosition.position, spawnPosition.rotation);

        spawnCount++;

        // 子クラス固有の初期化処理
        OnObjectSpawned(currentObject);

        // 生成したオブジェクトに消滅通知用のコンポーネントを追加する
        SpawnedObject spawnedObject = currentObject.GetComponent<SpawnedObject>();
        if(spawnedObject == null)
        {
            spawnedObject = currentObject.AddComponent<SpawnedObject>();
        }

        // 消滅したときに呼び出す処理を登録する
        spawnedObject.OnDestroyed = OnObjectDestroyed;

        Debug.Log($"スポーンした。現在のスポーン数: {spawnCount}", this);
    }

    /// <summary>
    /// オブジェクトがスポーンしたときに呼び出される仮想関数
    /// </summary>
    /// <param name="spawnedObject"></param>
    virtual protected void OnObjectSpawned(GameObject spawnedObject) { }

    /// <summary>
    /// オブジェクトが消滅したときに呼び出される処理
    /// </summary>
    private void OnObjectDestroyed()
    {
        // 再生成処理の重複を防ぐ
        if(isRespawning) { return; }
        isRespawning = true;

        // 子クラス固有の消滅処理を呼び出す
        OnObjectDestroyed(currentObject);

        if(isLoopSpawn || spawnCount < maxSpawnCount)
        {
            // 指定時間後に再生成する
            StartCoroutine(RespawnCoroutine());
        }
        else
        {
            Debug.Log($"{gameObject.name}: 最大生成数に達したため再生成しません", this);
        }
    }

    /// <summary>
    /// オブジェクトが消滅したときに呼び出される仮想関数
    /// </summary>
    /// <param name="destroyedObject"></param>
    virtual protected void OnObjectDestroyed(GameObject destroyedObject) { }

    /// <summary>
    /// リスポーンコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator RespawnCoroutine()
    {
        // オブジェクト消滅後、指定時間だけ待機する
        yield return new WaitForSeconds(spawnInterval);
        isRespawning = false;

        // 次のオブジェクトを生成する
        SpawnObject();
    }
}


