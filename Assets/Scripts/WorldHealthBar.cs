using UnityEngine;

/// <summary>Barra de vida pequena acima de um zumbi. So aparece depois que ele leva dano.</summary>
public class WorldHealthBar : MonoBehaviour
{
    private const float BarWidth = 0.9f;
    private const float BarHeight = 0.1f;
    private const float Gap = 0.12f;

    private static Sprite fillSprite;
    private static Sprite backSprite;

    private Transform owner;
    private SpriteRenderer ownerRenderer;
    private Transform fill;
    private GameObject root;

    public static WorldHealthBar Create(SpriteRenderer target)
    {
        GameObject rootObject = new GameObject("Health Bar");
        rootObject.transform.SetParent(target.transform, false);

        WorldHealthBar bar = rootObject.AddComponent<WorldHealthBar>();
        bar.owner = target.transform;
        bar.ownerRenderer = target;
        bar.root = rootObject;

        SpriteRenderer back = CreatePart(rootObject.transform, "Back", BackSprite(), new Color(0f, 0f, 0f, 0.7f), target.sortingOrder + 1, target.sortingLayerID);
        back.transform.localScale = new Vector3(BarWidth + 0.04f, BarHeight + 0.04f, 1f);

        SpriteRenderer front = CreatePart(rootObject.transform, "Fill", FillSprite(), new Color(0.85f, 0.15f, 0.15f, 1f), target.sortingOrder + 2, target.sortingLayerID);
        bar.fill = front.transform;
        bar.fill.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
        bar.fill.localScale = new Vector3(BarWidth, BarHeight, 1f);

        rootObject.SetActive(false);
        return bar;
    }

    public void SetFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        // So mostra quando ha dano e o zumbi continua vivo.
        bool visible = fraction > 0f && fraction < 1f;
        root.SetActive(visible);
        fill.localScale = new Vector3(BarWidth * fraction, BarHeight, 1f);
        if (visible)
        {
            LateUpdate();
        }
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    private void LateUpdate()
    {
        // A barra e filha do zumbi (que tem escala); compensa a escala para manter o tamanho no mundo.
        Vector3 scale = owner.localScale;
        transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, scale.x), 1f / Mathf.Max(0.0001f, scale.y), 1f);
        Vector3 position = owner.position;
        position.y = ownerRenderer.bounds.max.y + Gap;
        transform.position = position;
    }

    private static SpriteRenderer CreatePart(Transform parent, string name, Sprite sprite, Color color, int order, int layerId)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = layerId;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite FillSprite()
    {
        if (fillSprite == null)
        {
            // Pivo na esquerda para a barra encolher da direita para a esquerda.
            fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0.5f), 4f);
        }

        return fillSprite;
    }

    private static Sprite BackSprite()
    {
        if (backSprite == null)
        {
            backSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        return backSprite;
    }
}
