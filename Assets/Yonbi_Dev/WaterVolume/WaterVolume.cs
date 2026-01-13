using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WaterVolume : MonoBehaviour
{
    private CharacterController2D playerInWaterThisFrame;
    private Collider2D[] waterColliders;

    private void Awake()
    {
        waterColliders = GetComponents<Collider2D>();
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void LateUpdate()
    {
        // si aucun Stay ce frame = sorti de l'eau
        if (playerInWaterThisFrame != null)
        {
            playerInWaterThisFrame = null;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var cc = other.GetComponentInParent<CharacterController2D>();
        if (cc == null) return;

        if (other != cc.horizontalCapsuleCollider2D &&
            other != cc.verticalCapsuleCollider2D)
            return;

        playerInWaterThisFrame = cc;

        cc.SetWaterSurfaceY(GetSurfaceY());

        if (!cc.inWater)
            cc.SetInWater(true);  
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var cc = other.GetComponentInParent<CharacterController2D>();
        if (cc == null) return;

        if (other == cc.horizontalCapsuleCollider2D ||
            other == cc.verticalCapsuleCollider2D)
        {
            cc.SetInWater(false);
        }
    }

    private float GetSurfaceY()
    {
        if (waterColliders == null || waterColliders.Length == 0)
            return transform.position.y;

        float maxY = float.NegativeInfinity;
        for (int i = 0; i < waterColliders.Length; i++)
        {
            if (waterColliders[i] == null || !waterColliders[i].enabled)
                continue;
            maxY = Mathf.Max(maxY, waterColliders[i].bounds.max.y);
        }

        if (maxY == float.NegativeInfinity)
            return transform.position.y;

        return maxY;
    }
}
