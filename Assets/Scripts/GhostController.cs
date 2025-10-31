using UnityEngine;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    public enum Dir { None, Up, Left, Down, Right }
    public enum Mode { Normal, Scared, Recovering, Dead }

    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    public LayerMask wallMask;
    public LayerMask teleporterMask;
    public Collider2D ghostHouseArea;

    public float stepsPerSecond = 6f; // Base speed, will be set relative to PacStudent
    public float scaredSpeedScale = 0.5f; // 50% of normal speed
    public float recoveringSpeedScale = 0.5f; // Same as scared
    public int ghostId = 1;
    public Transform topExitPoint;
    public Transform bottomExitPoint;

    public Transform returnPoint;
    public float deadReturnSpeed = 6f;
    public float arriveRadius = 0.1f;
    public float respawnHoldSeconds = 1.5f;
    public float exitSpeedScale = 0.6f;
    public float deadDurationSeconds = 3f; // Ghost stays dead for 3 seconds

    public Animator animator;

    Collider2D col;
    Transform pac;
    Vector2Int gridPos;
    Vector3 fromWorld, toWorld;
    float t = 1f;
    Dir currentDir = Dir.Left;
    Dir lastDir = Dir.Left;
    Mode mode = Mode.Normal;
    bool hasExitedHouse = false;
    int lastPhase = -1;
    float exitHoldTimer = 0f;
    float deadTimer = 0f; // Timer to track how long ghost has been dead
    System.Random rng = new System.Random();

    public bool IsScared => mode == Mode.Scared;
    public bool IsRecovering => mode == Mode.Recovering;
    public bool IsDead => mode == Mode.Dead;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) pac = p.transform;
        ResetGrid(transform.position);
        ApplyAnimator();
    }

    static GameManager cachedGameManager;
    static float lastGameManagerCacheTime = -1f;
    static float gameManagerCacheRefreshInterval = 0.1f; // Cache for 0.1 seconds
    
    void Update()
    {
        // Check if game round has started - ghosts should not move until countdown finishes
        // Cache GameManager to avoid FindObjectOfType every frame
        if (cachedGameManager == null || Time.time - lastGameManagerCacheTime > gameManagerCacheRefreshInterval)
        {
            cachedGameManager = FindObjectOfType<GameManager>();
            lastGameManagerCacheTime = Time.time;
        }
        
        if (cachedGameManager != null && !cachedGameManager.HasStartedRound())
        {
            return; // Don't allow movement until round starts
        }

        if (mode == Mode.Dead)
        {
            DeadStraightReturn();
            return;
        }

        // Get PacStudent speed to calculate relative speeds
        float pacSpeed = GetPacStudentSpeed();
        float normalSpeed = pacSpeed * 0.9f; // Normal: 90% of PacStudent
        
        float spd;
        if (mode == Mode.Scared || mode == Mode.Recovering || mode == Mode.Dead)
        {
            spd = normalSpeed * scaredSpeedScale; // Scared/Recovery/Dead: 50% of Normal
        }
        else
        {
            spd = normalSpeed; // Normal: 90% of PacStudent
        }
        
        float stepT = spd * Time.deltaTime;

        SyncPhase();

        if (ghostHouseArea && !hasExitedHouse && ghostHouseArea.OverlapPoint(transform.position))
        {
            if (exitHoldTimer > 0f) { exitHoldTimer -= Time.deltaTime; return; }
            // Use slower exit speed based on current calculated speed
            float exitSpeed = spd * exitSpeedScale * Time.deltaTime;
            ExitHouse(exitSpeed);
            return;
        }

        if (t >= 1f) DecideAndStartNextStep();
        t += stepT;
        transform.position = Vector3.Lerp(fromWorld, toWorld, Mathf.Clamp01(t));
        if (t >= 1f) { fromWorld = toWorld = transform.position; t = 1f; }
    }

    void DeadStraightReturn()
    {
        if (!returnPoint) return;
        
        // Update dead timer
        deadTimer += Time.deltaTime;
        
        // Dead ghosts move at same speed as scared (50% of normal)
        float pacSpeed = GetPacStudentSpeed();
        float normalSpeed = pacSpeed * 0.9f; // Normal: 90% of PacStudent
        float deadSpeed = normalSpeed * scaredSpeedScale; // Dead: 50% of Normal (same as Scared)
        
        Vector3 target = returnPoint.position;
        Vector3 pos = transform.position;
        Vector3 next = Vector3.MoveTowards(pos, target, deadSpeed * cellSize * Time.deltaTime);
        Vector3 delta = target - pos;
        if (animator)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                currentDir = delta.x >= 0 ? Dir.Right : Dir.Left;
            else
                currentDir = delta.y >= 0 ? Dir.Up : Dir.Down;
            ApplyAnimator();
        }
        transform.position = next;
        
        // Only revive after reaching spawn AND being dead for 3 seconds
        if (Vector3.Distance(next, target) <= arriveRadius)
        {
            transform.position = target;
            ResetGrid(target);
            
            // Wait until ghost has been dead for 3 seconds before reviving
            if (deadTimer >= deadDurationSeconds)
            {
                ReviveAccordingToPhase();
                hasExitedHouse = false;
                exitHoldTimer = respawnHoldSeconds;
                TryResumeMusic();
                deadTimer = 0f; // Reset timer
            }
        }
    }

    void ExitHouse(float stepT)
    {
        Vector3 exitTarget = transform.position;
        bool useTop = (ghostId == 1 || ghostId == 3);
        if (useTop && topExitPoint) exitTarget = topExitPoint.position;
        else if (!useTop && bottomExitPoint) exitTarget = bottomExitPoint.position;

        Vector2Int exitCell = WorldToGrid(exitTarget);
        Vector2Int insideCell = exitCell + (useTop ? new Vector2Int(0, -1) : new Vector2Int(0, +1));
        Vector3 insideTarget = GridToWorld(insideCell);

        if (gridPos != insideCell)
        {
            MoveOneStepTowardWorld(insideTarget, false, false, false);
        }
        else
        {
            MoveOneStepTowardWorld(exitTarget, false, false, false);
            if (Vector3.Distance(transform.position, exitTarget) <= arriveRadius)
            {
                hasExitedHouse = true;
            }
        }

        t += stepT;
        transform.position = Vector3.Lerp(fromWorld, toWorld, Mathf.Clamp01(t));
        if (t >= 1f) { fromWorld = toWorld = transform.position; t = 1f; }
    }

    void DecideAndStartNextStep()
    {
        if (!pac) pac = GameObject.FindGameObjectWithTag("Player")?.transform;

        // When scared or recovering, all ghosts use Ghost 1 behavior (random direction that maintains or increases distance)
        if (mode == Mode.Scared || mode == Mode.Recovering)
        {
            StepRandomFartherOrEqual();
            return;
        }

        // Normal behavior based on ghostId (not behaviorIndex)
        switch (ghostId)
        {
            case 1:
                // Ghost 1: Random direction that maintains or increases distance from PacStudent
                StepRandomFartherOrEqual();
                break;
            case 2:
                // Ghost 2: Random direction that maintains or decreases distance from PacStudent
                StepRandomCloserOrEqual();
                break;
            case 3:
                // Ghost 3: Completely random direction
                StepRandom();
                break;
            case 4:
                // Ghost 4: Clockwise around the outside wall
                StepClockwiseWallFollowing();
                break;
            default:
                StepRandom();
                break;
        }
    }

    void StepRandom()
    {
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        Shuffle(options, rng);
        StartStep(options[0]);
    }

    void StepClockwiseWallFollowing()
    {
        // Ghost 4 behavior: Move clockwise around the outside wall
        // Clockwise priority: Right -> Down -> Left -> Up
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        
        // Priority order for clockwise movement
        Dir[] clockwiseOrder = new Dir[] { Dir.Right, Dir.Down, Dir.Left, Dir.Up };
        
        // First, try to move in clockwise priority order
        foreach (var dir in clockwiseOrder)
        {
            if (dir == Opposite(currentDir)) continue; // Don't reverse
            if (options.Contains(dir))
            {
                // Check if this direction follows a wall (prefer wall-following)
                Vector2Int nextCell = gridPos + ToDelta(dir);
                Vector3 nextWorld = GridToWorld(nextCell);
                
                // Prefer directions where there's a wall to the right (clockwise means keeping wall on right)
                Dir rightOfDir = GetRightDirection(dir);
                Vector2Int rightCell = nextCell + ToDelta(rightOfDir);
                if (!IsCellWalkable(rightCell, false, true))
                {
                    // Wall to the right - this is good for clockwise movement
                    StartStep(dir);
                    return;
                }
            }
        }
        
        // If no wall-following option, just use clockwise priority
        foreach (var dir in clockwiseOrder)
        {
            if (dir == Opposite(currentDir)) continue;
            if (options.Contains(dir))
            {
                StartStep(dir);
                return;
            }
        }
        
        // Fallback to any valid direction
        StartStep(options[0]);
    }
    
    Dir GetRightDirection(Dir facing)
    {
        // Returns the direction to the right of the given direction (clockwise)
        switch (facing)
        {
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
            default: return Dir.Right;
        }
    }

    void StepClosestToPac()
    {
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        Vector3 pacPos = pac ? pac.position : transform.position;
        Dir best = options[0];
        float bestD = float.MaxValue;
        foreach (var d in options)
        {
            Vector3 pos = GridToWorld(gridPos + ToDelta(d));
            float dist = Vector3.SqrMagnitude(pos - pacPos);
            if (dist < bestD) { bestD = dist; best = d; }
        }
        StartStep(best);
    }

    void StepRandomFartherOrEqual()
    {
        // Ghost 1 behavior: Random direction that maintains or increases distance from PacStudent
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        
        Vector3 pacPos = pac ? pac.position : transform.position;
        float currentDist = Vector3.SqrMagnitude(transform.position - pacPos);
        
        // Filter options to only include directions that maintain or increase distance
        List<Dir> validOptions = new List<Dir>();
        foreach (var d in options)
        {
            Vector3 pos = GridToWorld(gridPos + ToDelta(d));
            float newDist = Vector3.SqrMagnitude(pos - pacPos);
            if (newDist >= currentDist) // Maintains or increases distance
            {
                validOptions.Add(d);
            }
        }
        
        // If no valid options, allow all options (fallback)
        if (validOptions.Count == 0) validOptions = options;
        
        // Randomly select from valid options
        Shuffle(validOptions, rng);
        StartStep(validOptions[0]);
    }
    
    void StepRandomCloserOrEqual()
    {
        // Ghost 2 behavior: Random direction that maintains or decreases distance from PacStudent
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        
        Vector3 pacPos = pac ? pac.position : transform.position;
        float currentDist = Vector3.SqrMagnitude(transform.position - pacPos);
        
        // Filter options to only include directions that maintain or decrease distance
        List<Dir> validOptions = new List<Dir>();
        foreach (var d in options)
        {
            Vector3 pos = GridToWorld(gridPos + ToDelta(d));
            float newDist = Vector3.SqrMagnitude(pos - pacPos);
            if (newDist <= currentDist) // Maintains or decreases distance
            {
                validOptions.Add(d);
            }
        }
        
        // If no valid options, allow all options (fallback)
        if (validOptions.Count == 0) validOptions = options;
        
        // Randomly select from valid options
        Shuffle(validOptions, rng);
        StartStep(validOptions[0]);
    }
    
    void StepFarthestFromPac()
    {
        // Legacy method - keeping for compatibility but not used in main behavior
        var options = GetWalkableNeighbours(false, true);
        if (options.Count == 0) return;
        Vector3 pacPos = pac ? pac.position : transform.position;
        Dir best = options[0];
        float bestD = -1f;
        foreach (var d in options)
        {
            Vector3 pos = GridToWorld(gridPos + ToDelta(d));
            float dist = Vector3.SqrMagnitude(pos - pacPos);
            if (dist > bestD) { bestD = dist; best = d; }
        }
        StartStep(best);
    }

    List<Dir> GetWalkableNeighbours(bool canPassWalls, bool forbidEnteringHouse)
    {
        var list = new List<Dir>();
        foreach (Dir d in new[] { Dir.Up, Dir.Left, Dir.Down, Dir.Right })
        {
            if (d == Opposite(currentDir)) continue;
            Vector2Int next = gridPos + ToDelta(d);
            if (IsCellWalkable(next, canPassWalls, forbidEnteringHouse)) list.Add(d);
        }
        if (list.Count == 0)
        {
            foreach (Dir d in new[] { Dir.Up, Dir.Left, Dir.Down, Dir.Right })
            {
                Vector2Int next = gridPos + ToDelta(d);
                if (IsCellWalkable(next, canPassWalls, forbidEnteringHouse)) list.Add(d);
            }
        }
        return list;
    }

    void StartStep(Dir d)
    {
        lastDir = currentDir;
        currentDir = d;
        Vector2Int next = gridPos + ToDelta(d);
        fromWorld = GridToWorld(gridPos);
        toWorld = GridToWorld(next);
        gridPos = next;
        t = 0f;
        ApplyAnimator();
    }

    void MoveOneStepTowardWorld(Vector3 targetWorld, bool canPassWalls, bool forbidEnteringHouse, bool allowReverse)
    {
        var dirs = new List<Dir>(new[] { Dir.Up, Dir.Left, Dir.Down, Dir.Right });
        dirs.Sort((a, b) =>
        {
            Vector3 aPos = GridToWorld(gridPos + ToDelta(a));
            Vector3 bPos = GridToWorld(gridPos + ToDelta(b));
            float da = Vector3.SqrMagnitude(aPos - targetWorld);
            float db = Vector3.SqrMagnitude(bPos - targetWorld);
            return da.CompareTo(db);
        });
        foreach (var d in dirs)
        {
            if (!allowReverse && d == Opposite(currentDir)) continue;
            Vector2Int next = gridPos + ToDelta(d);
            if (!IsCellWalkable(next, canPassWalls, forbidEnteringHouse)) continue;
            StartStep(d);
            return;
        }
    }

    bool IsCellWalkable(Vector2Int cell, bool canPassWalls, bool forbidEnteringHouse)
    {
        Vector3 world = GridToWorld(cell);
        if (!canPassWalls)
        {
            var hit = Physics2D.OverlapPoint(world, wallMask);
            if (hit) return false;
        }
        
        // Ghosts cannot use teleporters - check both point overlap and area overlap for better detection
        var teleHit = Physics2D.OverlapPoint(world, teleporterMask);
        if (teleHit) return false;
        
        // Also check if any teleporter collider is in the cell area (more robust detection)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(world, cellSize * 0.4f);
        foreach (var col in colliders)
        {
            if (col != null && ((1 << col.gameObject.layer) & teleporterMask.value) != 0)
            {
                return false; // Teleporter found in this cell - not walkable
            }
        }
        
        if (forbidEnteringHouse && ghostHouseArea && ghostHouseArea.OverlapPoint(world)) return false;
        return true;
    }

    public void SetNormal()
    {
        mode = Mode.Normal;
        if (col) col.enabled = true;
        lastPhase = 0; // Update lastPhase to keep in sync with ScareManager
        ApplyAnimator();
        ResetGrid(transform.position);
    }

    public void SetScared()
    {
        mode = Mode.Scared;
        if (col) col.enabled = true;
        lastPhase = 1; // Update lastPhase to keep in sync with ScareManager
        ApplyAnimator();
        ResetGrid(transform.position);
    }

    public void SetRecovering()
    {
        mode = Mode.Recovering;
        if (col) col.enabled = true;
        lastPhase = 2; // Update lastPhase to keep in sync with ScareManager
        ApplyAnimator();
        ResetGrid(transform.position);
    }

    public void SetDead()
    {
        mode = Mode.Dead;
        if (col) col.enabled = false;
        if (animator) animator.SetBool("IsDead", true);
        hasExitedHouse = false;
        deadTimer = 0f; // Reset dead timer when ghost is killed
    }

    void SyncPhase()
    {
        // Only skip syncing if ghost is in the house AND has just revived (has hold timer active)
        // This prevents animation from stopping while ghost is exiting after revive
        // But still allows ghosts in house to become scared when power pellet is eaten
        if (!hasExitedHouse && exitHoldTimer > 0f && ghostHouseArea && ghostHouseArea.OverlapPoint(transform.position))
            return;
            
        int phase = ScareManagerPhase();
        if (phase == lastPhase) return;
        lastPhase = phase;
        if (mode == Mode.Dead) return;
        if (phase == 1) SetScared();
        else if (phase == 2) SetRecovering();
        else SetNormal();
    }

    static ScareManager cachedScareManager;
    static float lastCacheTime = -1f;
    static float cacheRefreshInterval = 0.1f; // Cache for 0.1 seconds
    
    int ScareManagerPhase()
    {
        // Cache ScareManager to avoid FindObjectOfType every frame
        if (cachedScareManager == null || Time.time - lastCacheTime > cacheRefreshInterval)
        {
            cachedScareManager = FindObjectOfType<ScareManager>();
            lastCacheTime = Time.time;
        }
        
        if (!cachedScareManager) return 0;
        switch (cachedScareManager.CurrentPhase)
        {
            case ScareManager.Phase.Scared: return 1;
            case ScareManager.Phase.Recovering: return 2;
            default: return 0;
        }
    }

    void ReviveAccordingToPhase()
    {
        int phase = ScareManagerPhase();
        lastPhase = phase; // Update lastPhase to prevent SyncPhase from immediately changing state
        if (phase == 1) SetScared();
        else if (phase == 2) SetRecovering();
        else SetNormal();
    }

    void TryResumeMusic()
    {
        var all = FindObjectsOfType<GhostController>();
        bool anyDead = false;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].IsDead) { anyDead = true; break; }
        if (anyDead) return;
        var bgm = FindObjectOfType<BgmPlayer>();
        if (!bgm) return;
        var sm = FindObjectOfType<ScareManager>();
        if (sm != null && sm.CurrentPhase != ScareManager.Phase.None) bgm.PlayScaredLoop();
        else bgm.PlayNormalLoop();
    }

    void ApplyAnimator()
    {
        if (!animator) return;
        animator.SetBool("IsDead", mode == Mode.Dead);
        animator.SetBool("IsScared", mode == Mode.Scared || mode == Mode.Recovering);
        animator.SetBool("IsRecovering", mode == Mode.Recovering);
        int dirInt = 0;
        if (currentDir == Dir.Up) dirInt = 0;
        else if (currentDir == Dir.Left) dirInt = 1;
        else if (currentDir == Dir.Down) dirInt = 2;
        else if (currentDir == Dir.Right) dirInt = 3;
        animator.SetInteger("Dir", dirInt);
    }

    void ResetGrid(Vector3 worldPos)
    {
        gridPos = WorldToGrid(worldPos);
        fromWorld = toWorld = GridToWorld(gridPos);
        t = 1f;
    }

    static Vector2Int ToDelta(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return new Vector2Int(0, 1);
            case Dir.Left: return new Vector2Int(-1, 0);
            case Dir.Down: return new Vector2Int(0, -1);
            case Dir.Right: return new Vector2Int(1, 0);
            default: return Vector2Int.zero;
        }
    }

    float GetPacStudentSpeed()
    {
        // Get PacStudent's current speed (accounting for speed boost)
        var pacController = pac ? pac.GetComponent<PacStudentController>() : null;
        if (pacController == null)
        {
            pacController = FindObjectOfType<PacStudentController>();
        }
        
        if (pacController != null)
        {
            float baseSpeed = pacController.speedCellsPerSecond;
            if (pacController.IsSpeedBoosted)
            {
                baseSpeed *= pacController.speedBoostMultiplier;
            }
            return baseSpeed;
        }
        
        // Fallback: return default speed if PacStudent not found
        return 8f; // Default PacStudent speed
    }

    static Dir Opposite(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Dir.Down;
            case Dir.Down: return Dir.Up;
            case Dir.Left: return Dir.Right;
            case Dir.Right: return Dir.Left;
            default: return Dir.None;
        }
    }

    Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(gridOrigin.x + cell.x * cellSize, gridOrigin.y + cell.y * cellSize, 0f);
    }

    Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3 p = world - gridOrigin;
        int x = Mathf.FloorToInt(p.x / cellSize);
        int y = Mathf.FloorToInt(p.y / cellSize);
        return new Vector2Int(x, y);
    }

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
