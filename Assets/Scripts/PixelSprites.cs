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

    private static Sprite heart;

    public static Sprite Heart
    {
        get
        {
            if (heart == null)
            {
                heart = BuildHeart();
            }

            return heart;
        }
    }

    private static Sprite BuildHeart()
    {
        int rows = HeartPattern.Length;
        int columns = HeartPattern[0].Length;
        int width = columns + 2;
        int height = rows + 2;

        Color fill = new Color(0.92f, 0.16f, 0.22f, 1f);
        Color shine = new Color(1f, 0.75f, 0.78f, 1f);
        Color outline = new Color(0.25f, 0.02f, 0.08f, 1f);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (HeartPattern[row][column] != 'X')
                {
                    continue;
                }

                // Linha 0 do padrao fica no topo; a textura tem y = 0 embaixo.
                int x = column + 1;
                int y = height - 2 - row;
                pixels[y * width + x] = fill;
            }
        }

        pixels[(height - 3) * width + 3] = shine;

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
            name = "Heart Pickup"
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
