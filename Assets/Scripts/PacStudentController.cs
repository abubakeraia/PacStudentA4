using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    public enum Dir { None, Up, Left, Down, Right }

    [Header("Grid")]
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Blocking (Layers)")]
    public LayerMask wallMask;
    public LayerMask ghostGateMask;
    public Collider2D ghostHouseArea;

    [Header("Movement")]
    public float speedCellsPerSecond = 8f;
    public Dir startFacing = Dir.Right;
    
    [Header("Speed Boost")]
    public float speedBoostMultiplier = 1.5f; // 1.5x speed boost
    public float speedBoostDuration = 5f; // Duration in seconds

    [Header("Start Override")]
    public bool useExplicitStartWorld = false;
    public Vector3 explicitStartWorld;

    [Header("Animator")]
    public Animator animator;

    public Dir lastInput = Dir.None;
    public Dir currentInput = Dir.None;

    [Header("Bump FX")]
    public AudioSource sfxSource;
    public AudioClip wallBumpClip;
    public ParticleSystem bumpVfxPrefab;
    public float bumpCooldown = 0.05f;

    float lastBumpTime = -999f;
    float lastTeleportTime = -999f;
    float teleportCooldown = 0.5f; // Prevent rapid teleportation
    
    float speedBoostTimer = 0f; // Timer for speed boost
    bool isSpeedBoosted = false;

    Vector2Int gridPos;
    Vector3 fromWorld, toWorld;
    float t = 1f;

    bool IsMoving => t < 1f;

    public Vector2Int CurrentCell => gridPos;
    public bool IsCurrentlyMoving => t < 1f;

    void Start()
    {
        if (useExplicitStartWorld)
        {
            var w = explicitStartWorld == Vector3.zero ? transform.position : explicitStartWorld;
            gridPos = WorldToGrid(w);
            transform.position = GridToWorld(gridPos);
        }
        else
        {
            gridPos = WorldToGrid(transform.position);
            transform.position = GridToWorld(gridPos);
        }

        fromWorld = toWorld = transform.position;
        t = 1f;
        SetAnimatorDir(startFacing);
        currentInput = Dir.None;
    }

    void Update()
    {
        // Update speed boost timer (regardless of movement state)
        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                isSpeedBoosted = false;
                speedBoostTimer = 0f;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Dir.Up;
        else if (Input.GetKeyDown(KeyCode.A)) lastInput = Dir.Left;
        else if (Input.GetKeyDown(KeyCode.S)) lastInput = Dir.Down;
        else if (Input.GetKeyDown(KeyCode.D)) lastInput = Dir.Right;

        // Check for teleporter collision manually as fallback if trigger doesn't work
        CheckTeleporterCollision();

        if (!IsMoving)
        {
            if (!TryStartMove(lastInput))
                TryStartMove(currentInput);
        }
        else
        {
            // Calculate speed with boost multiplier
            float currentSpeed = speedCellsPerSecond;
            bool isCurrentlyBoosted = isSpeedBoosted && speedBoostTimer > 0f;
            
            if (isCurrentlyBoosted)
            {
                currentSpeed *= speedBoostMultiplier;
            }
            
            float worldSpeed = currentSpeed * cellSize;
            float dist = Vector3.Distance(fromWorld, toWorld);
            float dur = Mathf.Max(0.0001f, dist / worldSpeed);
            
            // Accelerate t based on current speed (this handles speed changes mid-movement)
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(fromWorld, toWorld, Mathf.Clamp01(t));
        }

        if (!IsMoving && transform.position != toWorld)
            transform.position = toWorld;
    }
    
    void CheckTeleporterCollision()
    {
        // Manual check for teleporter colliders at player position
        // This is a fallback in case OnTriggerEnter2D doesn't fire
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, cellSize * 0.5f);
        foreach (var col in colliders)
        {
            if (col != null && col.gameObject.layer == LayerMask.NameToLayer("Teleporter"))
            {
                // Check cooldown
                if (Time.time - lastTeleportTime < teleportCooldown)
                    continue;
                    
                string teleporterName = col.name;
                if (teleporterName.Contains("Left"))
                {
                    TeleportTo("Right");
                    break;
                }
                else if (teleporterName.Contains("Right"))
                {
                    TeleportTo("Left");
                    break;
                }
            }
        }
    }

    bool TryStartMove(Dir dir)
    {
        if (dir == Dir.None) return false;

        Vector2Int next = gridPos + ToDelta(dir);

        if (!IsCellWalkable(next, dir))
        {
            PlayWallBump(dir);
            return false;
        }

        currentInput = dir;
        SetAnimatorDir(dir);

        fromWorld = GridToWorld(gridPos);
        toWorld = GridToWorld(next);
        t = 0f;
        gridPos = next;

        return true;
    }

    void SetAnimatorDir(Dir d)
    {
        if (!animator) return;
        int v = (d == Dir.Down) ? 0 :
                (d == Dir.Left) ? 1 :
                (d == Dir.Up) ? 2 : 3;
        animator.SetInteger("Dir", v);
    }

    public void Die()
    {
        if (animator) animator.SetBool("IsDead", true);
    }

    public void ResetTo(Vector3 worldPos)
    {
        transform.position = worldPos;
        gridPos = WorldToGrid(worldPos);
        fromWorld = toWorld = GridToWorld(gridPos);
        t = 1f;

        currentInput = Dir.None;
        lastInput = Dir.None;       
        SetAnimatorDir(startFacing);
        
        // Reset speed boost on respawn
        isSpeedBoosted = false;
        speedBoostTimer = 0f;
    }
    
    public void ApplySpeedBoost(float duration = -1f)
    {
        // Apply speed boost with specified duration (or use default)
        float boostDuration = duration > 0f ? duration : speedBoostDuration;
        bool wasBoosted = isSpeedBoosted;
        isSpeedBoosted = true;
        speedBoostTimer = boostDuration;
        
        float newSpeed = speedCellsPerSecond * speedBoostMultiplier;
        Debug.Log($"ApplySpeedBoost called! Duration: {boostDuration}s, Base speed: {speedCellsPerSecond}, Boosted speed: {newSpeed} cells/sec, Was already boosted: {wasBoosted}");
    }
    
    public bool IsSpeedBoosted => isSpeedBoosted;


    Vector2Int ToDelta(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return new Vector2Int(0, +1);
            case Dir.Left: return new Vector2Int(-1, 0);
            case Dir.Down: return new Vector2Int(0, -1);
            case Dir.Right: return new Vector2Int(+1, 0);
            default: return Vector2Int.zero;
        }
    }

    Vector3 GridToWorld(Vector2Int cell)
    {
        return gridOrigin + new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
    }

    Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3 p = world - gridOrigin;
        int x = Mathf.FloorToInt(p.x / cellSize);
        int y = Mathf.FloorToInt(p.y / cellSize);
        return new Vector2Int(x, y);
    }

    bool IsCellWalkable(Vector2Int targetCell, Dir enteringDir)
    {
        float probe = cellSize * 0.48f;
        Vector2 size = new Vector2(probe, probe);
        Vector2 center = GridToWorld(targetCell);

        if (Physics2D.OverlapBox(center, size, 0f, wallMask) != null)
            return false;

        if (ghostGateMask.value != 0)
        {
            var gateHit = Physics2D.OverlapBox(center, size, 0f, ghostGateMask);
            if (gateHit != null)
            {
                if (ghostHouseArea != null)
                {
                    bool fromInside = ghostHouseArea.OverlapPoint(GridToWorld(gridPos));
                    if (!fromInside) return false;
                }
                else return false;
            }
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var p = GridToWorld(WorldToGrid(transform.position));
        Gizmos.DrawWireCube(p, Vector3.one * cellSize * 0.9f);
    }

    void PlayWallBump(Dir dir)
    {
        if (Time.time - lastBumpTime < bumpCooldown) return;
        lastBumpTime = Time.time;

        Vector3 from = GridToWorld(gridPos);
        Vector3 dirV = DirToVec3(dir);
        float dist = cellSize * 0.55f;

        Vector3 hitPoint = from + dirV * dist;
        var hit = Physics2D.Raycast(from, dirV, dist, wallMask);
        if (hit.collider) hitPoint = hit.point;

        if (bumpVfxPrefab != null)
        {
            var vfx = Instantiate(bumpVfxPrefab, hitPoint, Quaternion.identity);
            var main = vfx.main; main.stopAction = ParticleSystemStopAction.Destroy;
            vfx.Play();
        }

        if (sfxSource != null && wallBumpClip != null)
            sfxSource.PlayOneShot(wallBumpClip);
    }

    Vector3 DirToVec3(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Vector3.up;
            case Dir.Left: return Vector3.left;
            case Dir.Down: return Vector3.down;
            case Dir.Right: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player is at a teleporter (end of tunnel)
        int teleporterLayer = LayerMask.NameToLayer("Teleporter");
        bool isTeleporterLayer = teleporterLayer != -1 && other.gameObject.layer == teleporterLayer;
        bool isTeleporterName = other.name.Contains("Teleporter") || other.name.Contains("teleporter");
        
        if (isTeleporterLayer || isTeleporterName)
        {
            // Check cooldown to prevent rapid teleportation
            if (Time.time - lastTeleportTime < teleportCooldown)
                return;
                
            Debug.Log($"OnTriggerEnter2D: Teleporter detected - {other.name}");
                
            // Determine which side we're teleporting from based on the collider name
            string teleporterName = other.name;
            if (teleporterName.Contains("Left") || teleporterName.Contains("left"))
            {
                TeleportTo("Right");
            }
            else if (teleporterName.Contains("Right") || teleporterName.Contains("right"))
            {
                TeleportTo("Left");
            }
            return;
        }
        
        // Check for bonus items on Bonus layer
        // Note: BonusWatermelonPickup component handles its own collision,
        // but we add this as a fallback in case component is missing
        int bonusLayer = LayerMask.NameToLayer("Bonus");
        if (bonusLayer != -1 && other.gameObject.layer == bonusLayer)
        {
            // Check if it already has BonusWatermelonPickup component (preferred method)
            var bonusPickup = other.GetComponent<BonusWatermelonPickup>();
            if (bonusPickup == null)
            {
                // Fallback: handle it here if no component attached
                var hud = FindObjectOfType<HUDController>();
                hud?.AddScore(100);
                Destroy(other.gameObject);
            }
            // If component exists, let it handle the collision
            return;
        }
        
        // Check for bonus life items on BonusLife layer
        // Note: BonusLifePickup component handles its own collision
        int bonusLifeLayer = LayerMask.NameToLayer("BonusLife");
        if (bonusLifeLayer != -1 && other.gameObject.layer == bonusLifeLayer)
        {
            // BonusLifePickup component should handle this, but we check as fallback
            var bonusLifePickup = other.GetComponent<BonusLifePickup>();
            if (bonusLifePickup == null)
            {
                // Fallback: check if bonus life is active and add life
                var manager = FindObjectOfType<BonusLifeManager>();
                if (manager != null && manager.IsBonusLifeActive(other.gameObject))
                {
                    var gameManager = FindObjectOfType<GameManager>();
                    gameManager?.AddLife();
                    manager.OnBonusLifeCollected(other.gameObject);
                }
            }
            // If component exists, let it handle the collision
            return;
        }
        
        // Check for speed banana items on SpeedBanana layer
        // Note: SpeedBananaPickup component handles its own collision
        int speedBananaLayer = LayerMask.NameToLayer("SpeedBanana");
        if (speedBananaLayer != -1 && other.gameObject.layer == speedBananaLayer)
        {
            // SpeedBananaPickup component should handle this, but we check as fallback
            var speedBananaPickup = other.GetComponent<SpeedBananaPickup>();
            if (speedBananaPickup == null)
            {
                // Fallback: apply speed boost if component is missing
                var manager = FindObjectOfType<SpeedBananaManager>();
                if (manager != null && manager.IsSpeedBananaActive(other.gameObject))
                {
                    ApplySpeedBoost(); // Apply the speed boost
                    manager.OnSpeedBananaCollected(other.gameObject);
                }
            }
            // If component exists, let it handle the collision
            return;
        }
    }

    void TeleportTo(string side)
    {
        // Update teleport cooldown
        lastTeleportTime = Time.time;
        
        // Find the target teleporter GameObject - try multiple naming patterns
        GameObject target = null;
        
        // Try exact name matches first
        string[] namePatterns = {
            "Teleporter" + side,
            "Teleporter_" + side,
            "teleporter" + side,
            "teleporter_" + side,
            side + "Teleporter",
            side + "_Teleporter"
        };
        
        foreach (string pattern in namePatterns)
        {
            target = GameObject.Find(pattern);
            if (target != null) break;
        }
        
        // If still not found, search all GameObjects in scene
        if (!target)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Teleporter") || obj.name.Contains("teleporter"))
                {
                    bool matchesSide = (side == "Left" && (obj.name.Contains("Left") || obj.name.Contains("left"))) ||
                                     (side == "Right" && (obj.name.Contains("Right") || obj.name.Contains("right")));
                    if (matchesSide)
                    {
                        target = obj;
                        break;
                    }
                }
            }
        }
        
        // Try finding by layer
        if (!target)
        {
            int teleporterLayer = LayerMask.NameToLayer("Teleporter");
            if (teleporterLayer != -1)
            {
                GameObject[] layerObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in layerObjects)
                {
                    if (obj.layer == teleporterLayer)
                    {
                        bool matchesSide = (side == "Left" && (obj.name.Contains("Left") || obj.name.Contains("left"))) ||
                                         (side == "Right" && (obj.name.Contains("Right") || obj.name.Contains("right")));
                        if (matchesSide)
                        {
                            target = obj;
                            break;
                        }
                    }
                }
            }
        }
        
        if (!target)
        {
            Debug.LogWarning($"Could not find teleporter target for side: {side}. Searched all GameObjects.");
            return;
        }

        Vector3 pos = transform.position;
        Vector3 dest = target.transform.position;
        dest.z = pos.z;

        Debug.Log($"Teleporting from {pos} to {dest} (side: {side})");

        // Teleport to the target position
        transform.position = dest;
        
        // Update grid position to match new world position
        gridPos = WorldToGrid(dest);
        fromWorld = toWorld = GridToWorld(gridPos);
        t = 1f;
        
        // Continue movement inward (towards the center of the level)
        // If we teleported to Left side, continue Right (inward towards center)
        // If we teleported to Right side, continue Left (inward towards center)
        Dir continueDir = (side == "Left") ? Dir.Right : Dir.Left;
        
        // Try to continue moving in the inward direction
        if (TryStartMove(continueDir))
        {
            // Successfully started moving inward
        }
        else
        {
            // If we can't move in that direction, try the current input direction
            if (currentInput != Dir.None && TryStartMove(currentInput))
            {
                // Successfully started with current input
            }
            else if (lastInput != Dir.None && TryStartMove(lastInput))
            {
                // Successfully started with last input
            }
        }
    }


}
