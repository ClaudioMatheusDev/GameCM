using System;
using UnityEngine;

/// <summary>Power-up temporario solto por zumbis: flutua, pisca antes de sumir e ativa o efeito ao toque.</summary>
public class PowerUpPickup : MonoBehaviour
{
    private const float Lifetime = 10f;
    private const float BlinkSeconds = 2.5f;
    private const float CollectRangeX = 0.9f;
    private const float CollectRangeY = 1.4f;
    private const float BobHeight = 0.1f;
    private const float BobSpeed = 4.5f;
    private const float ItemScale = 1.7f;

    private PlayerController player;
    private SpriteRenderer spriteRenderer;
    private PowerUpType type;
    private float age;
    private float baseY;

    public event Action<PowerUpPickup> Removed;

    public static PowerUpPickup Create(
        Vector3 feetPosition,
        PlayerController target,
        PowerUpType powerUpType,
        int sortingLayerId,
        int sortingOrder)
    {
        GameObject pickupObject = new GameObject("PowerUp Pickup " + powerUpType);
        pickupObject.transform.position = feetPosition;
        pickupObject.transform.localScale = Vector3.one * ItemScale;

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PixelSprites.PowerUp(powerUpType);
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        PowerUpPickup pickup = pickupObject.AddComponent<PowerUpPickup>();
        pickup.player = target;
        pickup.type = powerUpType;
        pickup.spriteRenderer = renderer;
        pickup.baseY = feetPosition.y + 0.1f;
        ParticleFx.PowerUpBurst(feetPosition + Vector3.up * 0.4f, PowerUpState.Tint(powerUpType));
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
        if (close)
        {
            player.ActivatePowerUp(type);
            Remove();
        }
    }

    private void Remove()
    {
        Removed?.Invoke(this);
        Destroy(gameObject);
    }
}
