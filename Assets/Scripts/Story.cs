using System.Collections.Generic;
using UnityEngine;

/// <summary>Um quadro de cutscene: imagem de fundo e texto que aparece letra a letra.</summary>
public sealed class CutsceneSlide
{
    public CutsceneSlide(Sprite image, string text)
    {
        Image = image;
        Text = text ?? string.Empty;
    }

    public Sprite Image { get; }
    public string Text { get; }
}

/// <summary>Textos da historia (abertura, final e nomes das fases). Os indices seguem a ordem dos fundos.</summary>
public static class Story
{
    public const int StreetStage = 0;
    public const int GraveyardStage = 1;
    public const int HospitalStage = 2;
    public const int LabStage = 3;
    public const int MetroStage = 4;
    public const int RooftopStage = 5;

    private static readonly string[] StageNames =
    {
        "Rua",
        "Cemitério",
        "Hospital",
        "Laboratório",
        "Metrô",
        "Telhado"
    };

    public static string StageName(int stageIndex)
    {
        return stageIndex >= 0 && stageIndex < StageNames.Length ? StageNames[stageIndex] : string.Empty;
    }

    public static List<CutsceneSlide> Intro(Sprite[] backgrounds)
    {
        return new List<CutsceneSlide>
        {
            Slide(backgrounds, LabStage,
                "Laboratório Helix, 3h12 da madrugada. Um experimento sai do controle e o alarme dispara."),
            Slide(backgrounds, HospitalStage,
                "Em poucas horas os hospitais lotam. E os mortos não ficam mortos por muito tempo."),
            Slide(backgrounds, StreetStage,
                "A cidade cai antes do amanhecer. Você é um dos poucos que ainda resistem."),
            Slide(backgrounds, RooftopStage,
                "O último helicóptero de resgate vai partir do alto do centro. Você precisa chegar até ele, custe o que custar.")
        };
    }

    public static List<CutsceneSlide> Ending(Sprite endingImage)
    {
        return new List<CutsceneSlide>
        {
            new CutsceneSlide(endingImage, "O helicóptero levanta voo enquanto o sol nasce sobre a cidade."),
            new CutsceneSlide(endingImage, "Você sobreviveu. Mas o que saiu daquele laboratório ainda está lá fora...")
        };
    }

    private static CutsceneSlide Slide(Sprite[] backgrounds, int index, string text)
    {
        Sprite image = backgrounds != null && index >= 0 && index < backgrounds.Length ? backgrounds[index] : null;
        return new CutsceneSlide(image, text);
    }
}

/// <summary>Regras de tempo das cutscenes (logica pura, testavel).</summary>
public static class CutsceneTiming
{
    public static int VisibleCharacters(float elapsedSeconds, float charactersPerSecond, int length)
    {
        if (length <= 0 || elapsedSeconds <= 0f || charactersPerSecond <= 0f)
        {
            return 0;
        }

        return Mathf.Min(length, Mathf.FloorToInt(elapsedSeconds * charactersPerSecond));
    }
}
