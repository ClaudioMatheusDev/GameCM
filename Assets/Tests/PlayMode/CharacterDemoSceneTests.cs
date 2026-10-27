using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CharacterDemoSceneTests
{
    private const string ScenePath = "Assets/Scenes/CharacterDemoScene.unity";

    private string originalPrefix;

    [SetUp]
    public void SetUp()
    {
        originalPrefix = GameProgress.KeyPrefix;
        GameProgress.KeyPrefix = "GameCM.PlayModeTests.";
        GameProgress.ResetAll();
    }

    [TearDown]
    public void TearDown()
    {
        GameClock.Reset();
        GameProgress.ResetAll();
        GameProgress.KeyPrefix = originalPrefix;
    }

    private static IEnumerator LoadScene()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator SceneBootsWithAllSystems()
    {
        yield return LoadScene();

        StageManager stage = Object.FindFirstObjectByType<StageManager>();
        Assert.IsNotNull(stage, "StageManager nao foi criado");
        Assert.AreEqual(4, stage.StageCount);
        Assert.AreEqual(GameState.Playing, stage.State);
        Assert.IsNotNull(Object.FindFirstObjectByType<PlayerController>());
        Assert.IsNotNull(Object.FindFirstObjectByType<GameHud>());
        Assert.IsNotNull(Object.FindFirstObjectByType<GameMenus>());
    }

    [UnityTest]
    public IEnumerator PlayerTakesDamageAndHealthPickupHealsIt()
    {
        yield return LoadScene();
        GameClock.Paused = false;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        int full = player.Health.Max;
        Assert.AreEqual(full, player.Health.Current);

        Assert.IsTrue(player.ReceiveHit(2, player.transform.position.x + 1f));
        Assert.AreEqual(full - 2, player.Health.Current);

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        HealthPickup.Spawn(player, player.transform.position, renderer.sortingLayerID, player.BaseSortingOrder - 1, 1);
        yield return null;
        yield return null;

        Assert.AreEqual(full - 1, player.Health.Current);
    }

    [UnityTest]
    public IEnumerator ClearingTheStageAndReachingTheEdgeAdvances()
    {
        yield return LoadScene();
        GameClock.Paused = false;

        StageManager stage = Object.FindFirstObjectByType<StageManager>();
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();

        int guard = 0;
        while (stage.State == GameState.Playing && stage.StageIndex == 0 && guard++ < 1200)
        {
            foreach (ZombieController zombie in Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
            {
                if (zombie.isActiveAndEnabled && zombie.IsAlive)
                {
                    zombie.ApplyHit(999, 0f);
                }
            }

            if (stage.StageCleared)
            {
                Vector3 position = player.transform.position;
                position.x = 100f;
                player.transform.position = position;
            }

            yield return null;
        }

        Assert.AreEqual(1, stage.StageIndex, "A fase 1 deveria ter avancado para a 2");
    }
}
