using UnityEngine;

/// <summary>Sprites pixel art pequenos gerados em codigo (sem depender de arquivos de imagem).</summary>
public static class PixelSprites
{
    private const float PixelsPerUnit = 16f;

    private static readonly string[] HeartPattern =
    {
        "..XX.XX..",
        ".XXXXXXX.",
        "XXXXXXXXX",
        "XXXXXXXXX",
        ".XXXXXXX.",
        "..XXXXX..",
        "...XXX...",
        "....X...."
    };

    private static readonly string[] BoltPattern =
    {
        ".....XX..",
        "....XX...",
        "...XX....",
        "..XXXXX..",
        ".....XX..",
        "....XX...",
        "...XX....",
        "..X......"
    };

    private static readonly string[] StrengthPattern =
    {
        "....X....",
        "...XXX...",
        "..XXXXX..",
        ".XXXXXXX.",
        "...XXX...",
        "...XXX...",
        "...XXX...",
        "...XXX..."
    };

    private static readonly string[] ShieldPattern =
    {
        ".XXXXXXX.",
        "XXXXXXXXX",
        "XXXXXXXXX",
        "XXXXXXXXX",
        ".XXXXXXX.",
        "..XXXXX..",
        "...XXX...",
        "....X...."
    };

    private static Sprite heart;
    private static readonly Sprite[] PowerUps = new Sprite[PowerUpState.TypeCount];

    public static Sprite Heart
    {
        get
        {
            if (heart == null)
            {
                heart = Build(HeartPattern, new Color(0.92f, 0.16f, 0.22f, 1f), new Color(0.25f, 0.02f, 0.08f, 1f), "Heart Pickup");
            }

            return heart;
        }
    }

    public static Sprite PowerUp(PowerUpType type)
    {
        int index = (int)type;
        if (PowerUps[index] == null)
        {
            string[] pattern;
            switch (type)
            {
                case PowerUpType.Haste:
                    pattern = BoltPattern;
                    break;
                case PowerUpType.Strength:
                    pattern = StrengthPattern;
                    break;
                default:
                    pattern = ShieldPattern;
                    break;
            }

            Color fill = PowerUpState.Tint(type);
            PowerUps[index] = Build(pattern, fill, new Color(0.08f, 0.08f, 0.12f, 1f), "PowerUp " + type);
        }

        return PowerUps[index];
    }

    private static Sprite Build(string[] pattern, Color fill, Color outline, string name)
    {
        int rows = pattern.Length;
        int columns = pattern[0].Length;
        int width = columns + 2;
        int height = rows + 2;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (pattern[row][column] != 'X')
                {
                    continue;
                }

                // Linha 0 do padrao fica no topo; a textura tem y = 0 embaixo.
                pixels[(height - 2 - row) * width + column + 1] = fill;
            }
        }

        // Brilho: um pixel mais claro perto do canto superior esquerdo da forma.
        Color shine = Color.Lerp(fill, Color.white, 0.6f);
        pixels[(height - 3) * width + 3] = pixels[(height - 3) * width + 3].a > 0f ? shine : pixels[(height - 3) * width + 3];

        Color[] result = (Color[])pixels.Clone();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a > 0f || !TouchesFilled(pixels, width, height, x, y))
                {
                    continue;
                }

                result[y * width + x] = outline;
            }
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = name
        };
        texture.SetPixels(result);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), PixelsPerUnit);
    }

    private static bool TouchesFilled(Color[] pixels, int width, int height, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    continue;
                }

                if (pixels[ny * width + nx].a > 0f)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
