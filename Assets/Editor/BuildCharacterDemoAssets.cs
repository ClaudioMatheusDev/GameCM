using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildCharacterDemoAssets
{
    private const string SpriteRoot = "Assets/spritsheet";
    private const string GeneratedRoot = "Assets/CharacterDemo";
    private const string AnimationRoot = GeneratedRoot + "/Animations";
    private const string AudioRoot = GeneratedRoot + "/Audio";
    private const string ThemeAudioPath = AudioRoot + "/BeepBox-Song.mp3";
    private const string SceneRoot = "Assets/Scenes";
    private const string BackgroundRoot = "Assets/Fundos";
    private const float PixelsPerUnit = 128f;
    private const float BackgroundPixelsPerUnit = 50f;
    private const int SpriteSheetCellSize = 256;
    private static readonly string[] BackgroundPaths =
    {
        BackgroundRoot + "/Init_cenario.jpg",
        BackgroundRoot + "/Seconde_cenario.jpg",
        BackgroundRoot + "/Thirt_Cenario.jpg",
        BackgroundRoot + "/four_cenario.jpg"
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

    [MenuItem("Tools/Character Demo/Build All Deliverables")]
    public static void BuildAllDeliverables()
    {
        Directory.CreateDirectory(GeneratedRoot);
        Directory.CreateDirectory(AnimationRoot);
        Directory.CreateDirectory(AudioRoot);
        Directory.CreateDirectory(SceneRoot);

        ConfigureSourceSprites();
        ConfigureBackgroundSprites();
        CreateSpriteSheet();
        AssetDatabase.Refresh();

        AnimatorController controller = CreateAnimationsAndController();
        AudioClip theme = LoadThemeAudio();
        Sprite[] backgrounds = LoadBackgroundSprites();
        CreateDemoScene(controller, theme, backgrounds);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Character Demo",
                "Entregáveis criados em Assets/CharacterDemo e cena demonstrativa em Assets/Scenes/CharacterDemoScene.unity.",
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

    private static void CreateDemoScene(AnimatorController controller, AudioClip theme, Sprite[] backgrounds)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CharacterDemoScene";

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

        GameObject character = new GameObject("Character Demo");
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

        CharacterDemoController demo = character.AddComponent<CharacterDemoController>();
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
        serializedDemo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SceneRoot + "/CharacterDemoScene.unity");
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

    private static double MidiToFrequency(int midi)
    {
        return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private static void WriteWav(string path, short[] samples, int sampleRate)
    {
        using BinaryWriter writer = new BinaryWriter(File.Create(path));
        int byteRate = sampleRate * 2;
        int dataLength = samples.Length * 2;

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        foreach (short sample in samples)
        {
            writer.Write(sample);
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
