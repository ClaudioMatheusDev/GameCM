using System;
using UnityEngine;

/// <summary>Item de cura solto por zumbis: flutua, pisca antes de sumir e cura o jogador ao toque.</summary>
public class HealthPickup : MonoBehaviour
{
    private const float Lifetime = 9f;
    private const float BlinkSeconds = 2.5f;
    private const float CollectRangeX = 0.9f;
    private const float CollectRangeY = 1.4f;
    private const float BobHeight = 0.08f;
    private const float BobSpeed = 4f;
    private const float ItemScale = 1.6f;

    private PlayerController player;
    private SpriteRenderer spriteRenderer;
    private int healAmount;
    private float age;
    private float baseY;

    public event Action<HealthPickup> Removed;

    public static HealthPickup Create(
        Vector3 feetPosition,
        PlayerController target,
        int amount,
        int sortingLayerId,
        int sortingOrder)
    {
        GameObject pickupObject = new GameObject("Health Pickup");
        pickupObject.transform.position = feetPosition;
        pickupObject.transform.localScale = Vector3.one * ItemScale;

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PixelSprites.Heart;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        HealthPickup pickup = pickupObject.AddComponent<HealthPickup>();
        pickup.player = target;
        pickup.healAmount = Mathf.Max(1, amount);
        pickup.spriteRenderer = renderer;
        pickup.baseY = feetPosition.y + 0.1f;
        ParticleFx.Sparkle(feetPosition + Vector3.up * 0.4f);
        return pickup;
    }

    private void Update()
    {
        age += Time.deltaTime;

        Vector3 position = transform.position;
        position.y = baseY + Mathf.Sin(age * BobSpeed) * BobHeight;
        transform.position = position;

        float remaining = Lifetime - age;
        spriteRenderer.enabled = remaining > BlinkSeconds || Mathf.Repeat(age, 0.2f) < 0.1f;
        if (remaining <= 0f)
        {
            Remove();
            return;
        }

        if (player == null || player.IsDead)
        {
            return;
        }

        Vector3 playerPosition = player.transform.position;
        bool close = Mathf.Abs(playerPosition.x - position.x) <= CollectRangeX
            && Mathf.Abs(playerPosition.y - baseY) <= CollectRangeY;

        // Com a vida cheia o item fica no chao esperando.
        if (close && player.Health.Heal(healAmount))
        {
            SfxPlayer.Play(Sfx.Pickup);
            ParticleFx.Heal(player.GetComponent<SpriteRenderer>().bounds.center);
            Remove();
        }
    }

    private void Remove()
    {
        Removed?.Invoke(this);
        Destroy(gameObject);
    }
}
