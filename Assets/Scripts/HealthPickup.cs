using UnityEngine;

/// <summary>Coracao de cura largado por zumbis: o jogador pega ao encostar e ele some depois de alguns segundos.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HealthPickup : MonoBehaviour
{
    private const float LifetimeSeconds = 8f;
    private const float BlinkSeconds = 2f;
    private const float PickupRange = 0.9f;
    private const float PickupMaxAirHeight = 1.2f;
    private const float BobHeight = 0.08f;
    private const float BobSpeed = 4f;
    private const int PixelsPerUnit = 12;

    private static Sprite heartSprite;

    private PlayerController player;
    private SpriteRenderer spriteRenderer;
    private float baseY;
    private float age;
    private int healAmount;

    public static HealthPickup Spawn(PlayerController target, Vector3 position, int sortingLayerId, int sortingOrder, int amount)
    {
        GameObject pickupObject = new GameObject("Health Pickup");
        pickupObject.transform.position = position;

        HealthPickup pickup = pickupObject.AddComponent<HealthPickup>();
        pickup.player = target;
        pickup.baseY = position.y;
        pickup.healAmount = Mathf.Max(1, amount);

        pickup.spriteRenderer = pickupObject.GetComponent<SpriteRenderer>();
        pickup.spriteRenderer.sprite = HeartSprite();
        pickup.spriteRenderer.sortingLayerID = sortingLayerId;
        pickup.spriteRenderer.sortingOrder = sortingOrder;
        return pickup;
    }

    private void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        if (age >= LifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 position = transform.position;
        position.y = baseY + (Mathf.Sin(age * BobSpeed) * 0.5f + 0.5f) * BobHeight;
        transform.position = position;

        Color color = Color.white;
        if (age > LifetimeSeconds - BlinkSeconds && Mathf.Repeat(age, 0.2f) < 0.1f)
        {
            color.a = 0.25f;
        }

        spriteRenderer.color = color;

        if (player.IsDead)
        {
            return;
        }

        bool close = Mathf.Abs(player.transform.position.x - position.x) <= PickupRange;
        if (close && player.AirHeight <= PickupMaxAirHeight && player.Health.Heal(healAmount))
        {
            SfxPlayer.Play(Sfx.UiClick);
            Destroy(gameObject);
        }
    }

    private static Sprite HeartSprite()
    {
        if (heartSprite != null)
        {
            return heartSprite;
        }

        string[] rows =
        {
            ".XX...XX.",
            "XXXX.XXXX",
            "XXXXXXXXX",
            "XXXXXXXXX",
            ".XXXXXXX.",
            "..XXXXX..",
            "...XXX...",
            "....X...."
        };

        int width = rows[0].Length + 2;
        int height = rows.Length + 2;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color fill = new Color(0.92f, 0.15f, 0.22f, 1f);
        Color shine = new Color(1f, 0.65f, 0.7f, 1f);
        Color outline = new Color(0.25f, 0.02f, 0.06f, 1f);

        bool[,] solid = new bool[width, height];
        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                solid[column + 1, height - 2 - row] = rows[row][column] == 'X';
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = clear;
                if (solid[x, y])
                {
                    color = fill;
                }
                else if (HasSolidNeighbor(solid, x, y, width, height))
                {
                    color = outline;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.SetPixel(2, height - 3, shine);
        texture.SetPixel(2, height - 4, shine);
        texture.Apply();

        heartSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), PixelsPerUnit);
        return heartSprite;
    }

    private static bool HasSolidNeighbor(bool[,] solid, int x, int y, int width, int height)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if ((dx != 0 || dy != 0) && nx >= 0 && ny >= 0 && nx < width && ny < height && solid[nx, ny])
                {
                    return true;
                }
            }
        }

        return false;
    }
}
