using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cobre um cenario largo repetindo o fundo lado a lado. Os blocos vizinhos sao espelhados,
/// assim as bordas coincidem e nao aparece emenda. O fundo original da cena e o bloco central.
/// </summary>
public sealed class ScrollingBackground
{
    private readonly SpriteRenderer main;
    private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();

    public ScrollingBackground(SpriteRenderer mainRenderer)
    {
        main = mainRenderer;
    }

    /// <summary>Quantos blocos extras sao precisos de cada lado para cobrir a distancia dada.</summary>
    public static int TilesNeeded(float coverDistance, float tileWidth)
    {
        if (tileWidth <= 0f)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.CeilToInt((coverDistance - tileWidth * 0.5f) / tileWidth));
    }

    public void Apply(Sprite sprite, float scale, float coverMinX, float coverMaxX)
    {
        main.sprite = sprite;
        Vector3 localScale = main.transform.localScale;
        localScale.x = scale;
        localScale.y = scale;
        main.transform.localScale = localScale;

        float width = sprite.bounds.size.x * scale;
        int left = TilesNeeded(-coverMinX, width);
        int right = TilesNeeded(coverMaxX, width);

        int used = 0;
        for (int k = -left; k <= right; k++)
        {
            if (k == 0)
            {
                continue;
            }

            SpriteRenderer tile = GetTile(used++);
            tile.sprite = sprite;
            tile.flipX = Mathf.Abs(k) % 2 == 1;
            tile.transform.localScale = localScale;

            Vector3 position = main.transform.position;
            position.x += k * width;
            tile.transform.position = position;
        }

        for (int i = used; i < tiles.Count; i++)
        {
            tiles[i].gameObject.SetActive(false);
        }
    }

    private SpriteRenderer GetTile(int index)
    {
        while (tiles.Count <= index)
        {
            GameObject tileObject = new GameObject("Background Tile");
            SpriteRenderer tile = tileObject.AddComponent<SpriteRenderer>();
            tile.sharedMaterial = main.sharedMaterial;
            tile.sortingLayerID = main.sortingLayerID;
            tile.sortingOrder = main.sortingOrder;
            tiles.Add(tile);
        }

        SpriteRenderer result = tiles[index];
        result.gameObject.SetActive(true);
        return result;
    }
}
