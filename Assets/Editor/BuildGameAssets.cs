using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildGameAssets
{
    private const string SpriteRoot = "Assets/Sprites";
    private const string GeneratedRoot = "Assets/Game";
    private const string AnimationRoot = GeneratedRoot + "/Animations";
    private const string AudioRoot = GeneratedRoot + "/Audio";
    private const string ThemeAudioPath = AudioRoot + "/AudioBackground.mp3";
    private const string SfxRoot = AudioRoot + "/Sons";
    private const string SceneRoot = "Assets/Scenes";
    private const string BackgroundRoot = "Assets/Backgrounds";
    private const string ZombieRoot = "Assets/Zombie";
    private const string ZombieWalkRoot = ZombieRoot + "/walk_left";
    private const string ZombieHurtRoot = ZombieRoot + "/hurt/up";
    private const string ZombieDeathRoot = ZombieRoot + "/Death";
    private const string ScenePath = SceneRoot + "/GameScene.unity";
    private const float PixelsPerUnit = 128f;
    private const float BackgroundPixelsPerUnit = 50f;
    private const float ZombiePixelsPerUnit = 100f;
    private const int SpriteSheetCellSize = 256;
    private static readonly string[] BackgroundPaths =
    {
        BackgroundRoot + "/Stage1.jpg",
        BackgroundRoot + "/Stage2.jpg",
        BackgroundRoot + "/Stage3.jpg",
        BackgroundRoot + "/Stage4.jpg"
    };

    private static readonly MoveSet[] Moves =
    {
        new("Idle", "idle", 4, 8f, true),
        new("Walk", "Walk", 9, 10f, true),
        new("Run", "Run", 9, 12f, true),
        new("Jump", "Jump", 5, 8f, false),
        new("Punch", "punch", 8, 14f, false),
        new("Power", "Power/pixilart-frames", 6, 10f, false),
        new("Hurt", "hurt", 5, 10f, false),
        new("Dead", "Dead", 6, 7f, false),
    };

    [MenuItem("Tools/GameCM/Build Sprites, Animations and Scene")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(GeneratedRoot);
        Directory.CreateDirectory(AnimationRoot);
        Directory.CreateDirectory(AudioRoot);
        Directory.CreateDirectory(SceneRoot);

        ConfigureSourceSprites();
        ConfigureBackgroundSprites();
        ConfigureZombieSprites();
        CreateSpriteSheet();
        AssetDatabase.Refresh();

        AnimatorController controller = CreateAnimationsAndController();
        AudioClip theme = LoadThemeAudio();
        Sprite[] backgrounds = LoadBackgroundSprites();
        Sprite[] zombieWalk = LoadZombieFrames(ZombieWalkRoot);
        Sprite[] zombieHurt = LoadZombieFrames(ZombieHurtRoot);
        Sprite[] zombieDeath = LoadZombieFrames(ZombieDeathRoot);
        CreateGameScene(controller, theme, backgrounds, zombieWalk, zombieHurt, zombieDeath, LoadSoundBank());
        RegisterSceneInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "GameCM",
                "Arquivos gerados em Assets/Game e cena demonstrativa em Assets/Scenes/GameScene.unity.",
                "OK");
        }
    }

    private static void ConfigureSourceSprites()
    {
        foreach (string path in Moves.SelectMany(GetFramePaths))
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void CreateSpriteSheet()
    {
        List<List<Texture2D>> rows = Moves.Select(move => GetFramePaths(move)
            .Select(path => LoadReadableTexture(path))
            .Where(texture => texture != null)
            .ToList()).ToList();

        int maxWidth = rows.SelectMany(row => row).Max(texture => texture.width);
        int maxHeight = rows.SelectMany(row => row).Max(texture => texture.height);
        float scale = Mathf.Min(1f, SpriteSheetCellSize / (float)Mathf.Max(maxWidth, maxHeight));
        int cellWidth = Mathf.Max(1, Mathf.RoundToInt(maxWidth * scale));
        int cellHeight = Mathf.Max(1, Mathf.RoundToInt(maxHeight * scale));
        int columns = rows.Max(row => row.Count);
        int width = columns * cellWidth;
        int height = rows.Count * cellHeight;

        Texture2D sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] clearPixels = Enumerable.Repeat(clear, width * height).ToArray();
        sheet.SetPixels(clearPixels);

        for (int row = 0; row < rows.Count; row++)
        {
            for (int column = 0; column < rows[row].Count; column++)
            {
                Texture2D frame = rows[row][column];
                int frameWidth = Mathf.Max(1, Mathf.RoundToInt(frame.width * scale));
                int frameHeight = Mathf.Max(1, Mathf.RoundToInt(frame.height * scale));
                int x = column * cellWidth + (cellWidth - frameWidth) / 2;
                int y = height - ((row + 1) * cellHeight) + (cellHeight - frameHeight) / 2;
                CopyScaled(frame, sheet, x, y, frameWidth, frameHeight);
            }
        }

        sheet.Apply();
        string path = GeneratedRoot + "/CharacterSpriteSheet.png";
        File.WriteAllBytes(path, sheet.EncodeToPNG());

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureBackgroundSprites()
    {
        foreach (string path in BackgroundPaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Imagem de fundo nao encontrada.", path);
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Multiple
                || !Mathf.Approximately(importer.spritePixelsPerUnit, BackgroundPixelsPerUnit)
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = BackgroundPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static Sprite[] LoadBackgroundSprites()
    {
        Sprite[] backgrounds = new Sprite[BackgroundPaths.Length];
        for (int i = 0; i < BackgroundPaths.Length; i++)
        {
            backgrounds[i] = AssetDatabase.LoadAllAssetsAtPath(BackgroundPaths[i])
                .OfType<Sprite>()
                .FirstOrDefault();
            if (backgrounds[i] == null)
            {
                throw new InvalidDataException("A imagem nao foi importada como sprite: " + BackgroundPaths[i]);
            }
        }

        return backgrounds;
    }

    private static IEnumerable<string> GetZombiePaths(string root)
    {
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("Pasta de sprites do zumbi nao encontrada: " + root);
        }

        string[] paths = Directory.GetFiles(root, "*.png")
            .OrderBy(path => int.TryParse(Path.GetFileNameWithoutExtension(path), out int frame)
                ? frame
                : int.MaxValue)
            .ToArray();

        if (paths.Length == 0)
        {
            throw new FileNotFoundException("Nenhum quadro do zumbi foi encontrado.", root);
        }

        return paths.Select(path => path.Replace("\\", "/"));
    }

    private static void ConfigureZombieSprites()
    {
        foreach (string path in new[] { ZombieWalkRoot, ZombieHurtRoot, ZombieDeathRoot }.SelectMany(GetZombiePaths))
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Sprite do zumbi nao encontrado.", path);
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || !Mathf.Approximately(importer.spritePixelsPerUnit, ZombiePixelsPerUnit)
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ZombiePixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static Sprite[] LoadZombieFrames(string root)
    {
        Sprite[] frames = GetZombiePaths(root)
            .Select(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault())
            .Where(sprite => sprite != null)
            .ToArray();

        if (frames.Length == 0)
        {
            throw new InvalidDataException("Os quadros do zumbi nao foram importados como sprites: " + root);
        }

        return frames;
    }

    private static void RegisterSceneInBuildSettings()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static AnimatorController CreateAnimationsAndController()
    {
        string controllerPath = AnimationRoot + "/CharacterAnimator.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = Array.Empty<ChildAnimatorState>();
        stateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();

        for (int i = 0; i < Moves.Length; i++)
        {
            MoveSet move = Moves[i];
            AnimationClip clip = CreateClip(move);
            AnimatorState state = stateMachine.AddState(move.Name);
            state.motion = clip;

            if (i == 0)
            {
                stateMachine.defaultState = state;
            }

            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.04f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, i, "State");
        }

        return controller;
    }

    private static AnimationClip CreateClip(MoveSet move)
    {
        string clipPath = AnimationRoot + "/" + move.Name + ".anim";
        AssetDatabase.DeleteAsset(clipPath);
        AnimationClip clip = new AnimationClip
        {
            frameRate = move.FrameRate
        };

        List<Sprite> sprites = GetFramePaths(move)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
            .Where(sprite => sprite != null)
            .ToList();

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count + (move.Looping ? 1 : 0)];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / move.FrameRate,
                value = sprites[i]
            };
        }

        if (move.Looping && sprites.Count > 0)
        {
            keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe
            {
                time = sprites.Count / move.FrameRate,
                value = sprites[0]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = move.Looping;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static readonly string[] SoundFields = { "punch", "power", "jump", "hurt", "death", "uiClick", "uiSelect", "pickup" };

    private static readonly string[] SoundFiles =
    {
        "punch-song.mp3", "power-song.mp3", "JumpSonge.mp3", "hurt-song.mp3", "death-song.mp3",
        "menu-click.wav", "menu-select.wav", "pickup.wav"
    };

    private static AudioClip[] LoadSoundBank()
    {
        return SoundFiles.Select(file =>
        {
            string path = SfxRoot + "/" + file;
            AssetDatabase.ImportAsset(path);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new FileNotFoundException("Efeito sonoro nao encontrado.", path);
            }

            return clip;
        }).ToArray();
    }

    private static AudioClip LoadThemeAudio()
    {
        AssetDatabase.ImportAsset(ThemeAudioPath);
        AudioClip theme = AssetDatabase.LoadAssetAtPath<AudioClip>(ThemeAudioPath);
        if (theme == null)
        {
            throw new FileNotFoundException("Arquivo de trilha sonora nao encontrado.", ThemeAudioPath);
        }

        return theme;
    }

    private static void CreateGameScene(
        AnimatorController controller,
        AudioClip theme,
        Sprite[] backgrounds,
        Sprite[] zombieWalk,
        Sprite[] zombieHurt,
        Sprite[] zombieDeath,
        AudioClip[] sounds)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "GameScene";

        Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 8.0f;
        camera.transform.position = new Vector3(0f, 0.5f, -10f);
        camera.backgroundColor = new Color(0.10f, 0.14f, 0.18f);
        camera.gameObject.AddComponent<AudioListener>();

        GameObject backgroundObject = new GameObject("Stage Background");
        SpriteRenderer backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgrounds[0];
        backgroundRenderer.sortingOrder = -100;
        float backgroundScale = camera.orthographicSize * 2f / backgroundRenderer.sprite.bounds.size.y;
        backgroundObject.transform.localScale = Vector3.one * backgroundScale;
        backgroundObject.transform.position = new Vector3(0f, 0f, 1f);

        GameObject character = new GameObject("Player");
        character.transform.position = new Vector3(-10.5f, -2f, 0f);

        SpriteRenderer renderer = character.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetFramePaths(Moves[0]).First());
        renderer.sortingOrder = 10;

        Animator animator = character.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        AudioSource audio = character.AddComponent<AudioSource>();
        audio.clip = theme;
        audio.loop = true;
        audio.playOnAwake = true;
        audio.volume = 0.45f;

        GameBootstrap demo = character.AddComponent<GameBootstrap>();
        SerializedObject serializedDemo = new SerializedObject(demo);
        serializedDemo.FindProperty("animator").objectReferenceValue = animator;
        serializedDemo.FindProperty("themeSource").objectReferenceValue = audio;
        serializedDemo.FindProperty("stageBackground").objectReferenceValue = backgroundRenderer;
        SerializedProperty backgroundArray = serializedDemo.FindProperty("stageBackgrounds");
        backgroundArray.arraySize = backgrounds.Length;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            backgroundArray.GetArrayElementAtIndex(i).objectReferenceValue = backgrounds[i];
        }

        AssignSprites(serializedDemo.FindProperty("zombieWalkFrames"), zombieWalk);
        AssignSprites(serializedDemo.FindProperty("zombieHurtFrames"), zombieHurt);
        AssignSprites(serializedDemo.FindProperty("zombieDeathFrames"), zombieDeath);
        serializedDemo.FindProperty("stageSettings.zombieHeightRatio").floatValue = 0.5f;
        for (int i = 0; i < SoundFields.Length; i++)
        {
            serializedDemo.FindProperty("sounds." + SoundFields[i]).objectReferenceValue = sounds[i];
        }

        serializedDemo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void AssignSprites(SerializedProperty array, Sprite[] sprites)
    {
        array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private static IEnumerable<string> GetFramePaths(MoveSet move)
    {
        string folder = SpriteRoot + "/" + move.Folder;
        return Directory.GetFiles(folder, "*.png")
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => ExtractFrameNumber(path))
            .Take(move.FrameCount)
            .Select(path => path.Replace("\\", "/"));
    }

    private static int ExtractFrameNumber(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        int dash = name.LastIndexOf('-');
        return dash >= 0 && int.TryParse(name.Substring(dash + 1), out int number) ? number : 0;
    }

    private static Texture2D LoadReadableTexture(string assetPath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        byte[] data = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        return texture.LoadImage(data) ? texture : null;
    }

    private static void CopyScaled(Texture2D source, Texture2D target, int startX, int startY, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.FloorToInt(y / (float)height * source.height), 0, source.height - 1);
            for (int x = 0; x < width; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt(x / (float)width * source.width), 0, source.width - 1);
                target.SetPixel(startX + x, startY + y, source.GetPixel(sourceX, sourceY));
            }
        }
    }

    private readonly struct MoveSet
    {
        public readonly string Name;
        public readonly string Folder;
        public readonly int FrameCount;
        public readonly float FrameRate;
        public readonly bool Looping;

        public MoveSet(string name, string folder, int frameCount, float frameRate, bool looping)
        {
            Name = name;
            Folder = folder;
            FrameCount = frameCount;
            FrameRate = frameRate;
            Looping = looping;
        }
    }
}
