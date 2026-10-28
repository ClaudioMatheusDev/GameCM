using System;
using UnityEngine;

public enum BindAction
{
    Left,
    Right,
    Jump,
    Punch,
    Power,
    Sprint
}

/// <summary>Teclas do teclado configuraveis (salvas em PlayerPrefs). Setas e gamepad continuam fixos.</summary>
public static class KeyBindings
{
    private const string KeyboardPrefix = "<Keyboard>/";
    private const string ReservedPath = "<Keyboard>/escape";

    private static readonly string[] Defaults =
    {
        "<Keyboard>/a",
        "<Keyboard>/d",
        "<Keyboard>/space",
        "<Keyboard>/j",
        "<Keyboard>/k",
        "<Keyboard>/leftShift"
    };

    private static string[] current;

    /// <summary>Aumenta a cada mudanca; quem monta textos com as teclas usa para saber quando refazer.</summary>
    public static int Version { get; private set; }

    public static int Count => Defaults.Length;

    public static string Get(BindAction action)
    {
        EnsureLoaded();
        return current[(int)action];
    }

    public static string GetDefault(BindAction action)
    {
        return Defaults[(int)action];
    }

    /// <summary>Esc e reservado para voltar/pausar e nao pode ser atribuido.</summary>
    public static bool IsReserved(string path)
    {
        return string.Equals(path, ReservedPath, StringComparison.Ordinal);
    }

    /// <summary>Atribui a tecla; se outra acao ja a usava, as duas trocam de tecla.</summary>
    public static void Set(BindAction action, string path)
    {
        if (string.IsNullOrEmpty(path) || IsReserved(path))
        {
            return;
        }

        EnsureLoaded();
        current = Assign(current, action, path);
        Save();
    }

    public static void ResetToDefaults()
    {
        current = (string[])Defaults.Clone();
        Save();
    }

    /// <summary>Forca a releitura das teclas salvas (usado quando o prefixo de chaves muda nos testes).</summary>
    public static void Reload()
    {
        current = null;
        Version++;
    }

    /// <summary>Calcula as novas teclas sem tocar em PlayerPrefs (logica pura, testavel).</summary>
    public static string[] Assign(string[] bindings, BindAction action, string path)
    {
        string[] result = (string[])bindings.Clone();
        int index = (int)action;
        int existing = Array.IndexOf(result, path);
        if (existing >= 0 && existing != index)
        {
            result[existing] = result[index];
        }

        result[index] = path;
        return result;
    }

    public static string DisplayName(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "-";
        }

        string name = path.StartsWith(KeyboardPrefix, StringComparison.Ordinal)
            ? path.Substring(KeyboardPrefix.Length)
            : path;

        switch (name)
        {
            case "space":
                return "Espaço";
            case "leftShift":
                return "Shift esq.";
            case "rightShift":
                return "Shift dir.";
            case "leftCtrl":
                return "Ctrl esq.";
            case "rightCtrl":
                return "Ctrl dir.";
            case "leftAlt":
                return "Alt esq.";
            case "rightAlt":
                return "Alt dir.";
            case "enter":
                return "Enter";
            case "tab":
                return "Tab";
            case "backspace":
                return "Backspace";
            case "upArrow":
                return "Seta cima";
            case "downArrow":
                return "Seta baixo";
            case "leftArrow":
                return "Seta esq.";
            case "rightArrow":
                return "Seta dir.";
        }

        if (name.StartsWith("digit", StringComparison.Ordinal))
        {
            return name.Substring(5);
        }

        if (name.StartsWith("numpad", StringComparison.Ordinal))
        {
            return "Num " + name.Substring(6);
        }

        return name.Length == 1 ? name.ToUpperInvariant() : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static string ActionLabel(BindAction action)
    {
        switch (action)
        {
            case BindAction.Left:
                return "Esquerda";
            case BindAction.Right:
                return "Direita";
            case BindAction.Jump:
                return "Pular";
            case BindAction.Punch:
                return "Soco";
            case BindAction.Power:
                return "Poder";
            default:
                return "Correr";
        }
    }

    private static string StorageKey(int index)
    {
        return GameProgress.KeyPrefix + "Bind." + (BindAction)index;
    }

    private static void EnsureLoaded()
    {
        if (current != null)
        {
            return;
        }

        current = new string[Defaults.Length];
        for (int i = 0; i < Defaults.Length; i++)
        {
            current[i] = PlayerPrefs.GetString(StorageKey(i), Defaults[i]);
        }
    }

    private static void Save()
    {
        for (int i = 0; i < current.Length; i++)
        {
            PlayerPrefs.SetString(StorageKey(i), current[i]);
        }

        PlayerPrefs.Save();
        Version++;
    }
}
