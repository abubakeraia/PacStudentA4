using UnityEngine;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    public enum Dir { None, Up, Left, Down, Right }
    public enum Mode { Normal, Scared, Recovering, Dead }

    [Header("Grid")]
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Blocking")]
    public LayerMask wallMask;
    public LayerMask teleporterMask;
    public Collider2D ghostHouseArea;
    public Transform houseSpawnPoint;
    public Transform topExitPoint;
    public Transform bottomExitPoint;
    public int ghostId = 1;

    [Header("Actors")]
    public Transform pac;
    public float pacSpeedCellsPerSecond = 8f;

    [Header("Speeds")]
    public float normalPctOfPac = 0.9f;
    public float scaredPctOfNormal = 0.5f;

    [Header("Animator")]
    public Animator animator;

    [Header("Behavior")]
    public int behaviorIndex = 1;

    [Header("Dead Movement")]
    public float arriveRadius = 0.15f;
    public Transform returnPoint;

    Vector2Int gridPos;
    Vector3 fromWorld, toWorld;
    float t = 1f;
    Dir currentDir = Dir.Left;
    Dir lastDir = Dir.Left;
    Mode mode = Mode.Normal;
    bool hasExitedHouse = false;

    int hDir, hScared, hRecov, hDead;
    int lastPhase = -1;

    Collider2D col;

    void Awake()
    {
        hDir = Animator.StringToHash("Dir");
        hScared = Animator.StringToHash("IsScared");
        hRecov = Animator.StringToHash("IsRecovering");
        hDead = Animator.StringToHash("IsDead");
        if (!animator) animator = GetComponent<Animator>();
        if (!pac)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) pac = p.transform;
        }
        col = GetComponent<Collider2D>();
        if (!returnPoint) returnPoint = houseSpawnPoint;

        gridPos = WorldToGrid(transform.position);
        fromWorld = toWorld = GridToWorld(gridPos);
        t = 1f;
        UpdateAnimatorDir(currentDir);
        ApplyAnimator();
    }

    void Update()
    {
        if (!pac) return;

        if (mode != Mode.Dead) SyncPhase();

        if (mode == Mode.Dead)
        {
            DeadMove();
            return;
        }

        if (!IsMoving())
        {
            DecideAndStartNextStep();
        }
        else
        {
            float worldSpeed = CurrentSpeedCellsPerSecond() * cellSize;
            float dist = Vector3.Distance(fromWorld, toWorld);
            float dur = Mathf.Max(0.0001f, dist / worldSpeed);
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(fromWorld, toWorld, Mathf.Clamp01(t));
        }

        if (!IsMoving() && transform.position != toWorld)
            transform.position = toWorld;
    }

    void SyncPhase()
    {
        int phase = ScareManagerPhase();
        if (phase == lastPhase) return;
        lastPhase = phase;
        if (mode == Mode.Dead) return;
        if (phase == 1) SetScared();
        else if (phase == 2) SetRecovering();
        else SetNormal();
    }

    float CurrentSpeedCellsPerSecond()
    {
        float normal = pacSpeedCellsPerSecond * normalPctOfPac;
        float scared = normal * scaredPctOfNormal;
        switch (mode)
        {
            case Mode.Normal: return normal;
            case Mode.Scared: return scared;
            case Mode.Recovering: return scared;
            case Mode.Dead: return scared;
        }
        return normal;
    }

    bool IsMoving() => t < 1f;

    void DeadMove()
    {
        Vector3 target = houseSpawnPoint ? houseSpawnPoint.position : transform.position;
        float speed = CurrentSpeedCellsPerSecond() * cellSize;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        gridPos = WorldToGrid(transform.position);

        if (ghostHouseArea && ghostHouseArea.OverlapPoint(transform.position))
        {
            Vector3 rp = returnPoint ? returnPoint.position : target;
            if (Vector3.Distance(transform.position, rp) <= arriveRadius)
            {
                transform.position = rp;
                ReviveAccordingToPhase();
                ResetGrid(transform.position);
                hasExitedHouse = false;
                TryResumeMusic();
            }
        }
    }

    void DecideAndStartNextStep()
    {
        if (!hasExitedHouse && ghostHouseArea)
        {
            if (!ghostHouseArea.OverlapPoint(transform.position)) hasExitedHouse = true;
            else
            {
                transform.position = GridToWorld(WorldToGrid(transform.position));
                ResetGrid(transform.position);

                Vector3 exitTarget =
                    ((ghostId == 1 || ghostId == 3) && topExitPoint) ? topExitPoint.position :
                    ((ghostId == 2 || ghostId == 4) && bottomExitPoint) ? bottomExitPoint.position :
                    transform.position;

                MoveOneStepTowardWorld(exitTarget, false, false, true);
                return;
            }
        }

        switch (mode)
        {
            case Mode.Normal: MoveOneStep_Normal(); break;
            case Mode.Scared:
            case Mode.Recovering: MoveOneStep_ScaredLike(); break;
        }
    }

    void MoveOneStep_Normal()
    {
        var options = ValidOptions(true, true);
        if (options.Count == 0) { options = ValidOptions(false, true); if (options.Count == 0) return; }

        System.Random rng = new System.Random();
        Vector2Int best = options[0];

        if (behaviorIndex == 1)
        {
            float cur = DistToPac(gridPos);
            Shuffle(options, rng);
            float bestScore = float.NegativeInfinity;
            foreach (var c in options)
            {
                float d = DistToPac(c);
                if (d > bestScore && d >= cur) { bestScore = d; best = c; }
            }
        }
        else if (behaviorIndex == 2)
        {
            float cur = DistToPac(gridPos);
            Shuffle(options, rng);
            float bestScore = float.PositiveInfinity;
            foreach (var c in options)
            {
                float d = DistToPac(c);
                if (d < bestScore && d <= cur) { bestScore = d; best = c; }
            }
        }
        else if (behaviorIndex == 3)
        {
            Shuffle(options, rng);
            best = options[0];
        }
        else
        {
            Vector2Int cwPref = ClockwisePreferred();
            foreach (var c in options) { if (c == cwPref) { best = c; break; } }
        }

        StartStepTo(best);
    }

    void MoveOneStep_ScaredLike()
    {
        var options = ValidOptions(true, true);
        if (options.Count == 0) { options = ValidOptions(false, true); if (options.Count == 0) return; }

        float cur = DistToPac(gridPos);
        System.Random rng = new System.Random();
        Shuffle(options, rng);

        float bestScore = float.NegativeInfinity;
        Vector2Int best = options[0];
        foreach (var c in options)
        {
            float d = DistToPac(c);
            if (d > bestScore && d >= cur) { bestScore = d; best = c; }
        }
        StartStepTo(best);
    }

    void MoveOneStepTowardWorld(Vector3 worldTarget, bool canPassWalls, bool forbidEnteringHouse, bool allowReverse)
    {
        Vector2Int targetCell = WorldToGrid(worldTarget);
        Vector2Int delta = targetCell - gridPos;
        Dir dir = AbsDir(delta);
        if (dir == Dir.None) dir = BiasDirToTarget(worldTarget);

        Vector2Int next = gridPos + ToDelta(dir);

        if (!canPassWalls)
        {
            if (!IsCellWalkable(next, dir, forbidEnteringHouse))
            {
                foreach (Dir d in new[] { Dir.Up, Dir.Left, Dir.Down, Dir.Right })
                {
                    if (!allowReverse && d == Opposite(currentDir)) continue;
                    var n2 = gridPos + ToDelta(d);
                    if (IsCellWalkable(n2, d, forbidEnteringHouse)) { next = n2; dir = d; break; }
                }
            }
        }

        StartStepTo(next, dir);
    }

    void StartStepTo(Vector2Int cell, Dir forcedDir = Dir.None)
    {
        Dir d = forcedDir == Dir.None ? DirFromTo(gridPos, cell) : forcedDir;
        if (d == Dir.None) return;

        lastDir = currentDir;
        currentDir = d;
        UpdateAnimatorDir(d);

        fromWorld = GridToWorld(gridPos);
        toWorld = GridToWorld(cell);
        t = 0f;
        gridPos = cell;
    }

    List<Vector2Int> ValidOptions(bool noReverse, bool forbidEnteringHouse)
    {
        var list = new List<Vector2Int>();
        foreach (Dir d in new[] { Dir.Up, Dir.Left, Dir.Down, Dir.Right })
        {
            if (noReverse && d == Opposite(currentDir)) continue;
            var n = gridPos + ToDelta(d);
            if (IsCellWalkable(n, d, forbidEnteringHouse)) list.Add(n);
        }
        return list;
    }

    bool IsCellWalkable(Vector2Int cell, Dir enteringDir, bool forbidEnteringHouse)
    {
        if (mode == Mode.Dead) return true;

        Vector2 center = GridToWorld(cell);
        float probe = cellSize * 0.48f;
        int mask = wallMask | teleporterMask;

        if (Physics2D.OverlapBox(center, new Vector2(probe, probe), 0f, mask) != null)
            return false;

        Vector2 from = GridToWorld(gridPos);
        Vector2 dir = (center - from).normalized;
        float dist = Vector2.Distance(from, center);
        Vector2 castSize = new Vector2(cellSize * 0.45f, cellSize * 0.45f);
        var hit = Physics2D.BoxCast(from, castSize, 0f, dir, dist, mask);
        if (hit.collider != null) return false;

        if (forbidEnteringHouse && hasExitedHouse && ghostHouseArea && ghostHouseArea.OverlapPoint(center))
            return false;

        return true;
    }

    float DistToPac(Vector2Int cell)
    {
        if (!pac) return 0f;
        Vector3 p = pac.position;
        Vector3 w = GridToWorld(cell);
        return Vector3.Distance(w, p);
    }

    Vector2Int ClockwisePreferred()
    {
        Vector2Int[] order = new[] { ToDelta(Dir.Right), ToDelta(Dir.Down), ToDelta(Dir.Left), ToDelta(Dir.Up) };
        for (int i = 0; i < order.Length; i++)
        {
            Vector2Int candidate = gridPos + order[i];
            if (IsCellWalkable(candidate, DirFromTo(gridPos, candidate), true)) return candidate;
        }
        return gridPos + ToDelta(currentDir);
    }

    Dir DirFromTo(Vector2Int a, Vector2Int b)
    {
        Vector2Int d = b - a;
        if (d.y > 0) return Dir.Up;
        if (d.y < 0) return Dir.Down;
        if (d.x > 0) return Dir.Right;
        if (d.x < 0) return Dir.Left;
        return Dir.None;
    }

    Dir AbsDir(Vector2Int d)
    {
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? Dir.Right : Dir.Left;
        if (Mathf.Abs(d.y) > Mathf.Abs(d.x)) return d.y > 0 ? Dir.Up : Dir.Down;
        return Dir.None;
    }

    Dir BiasDirToTarget(Vector3 worldTarget)
    {
        Vector3 w = GridToWorld(gridPos);
        Vector3 d = (worldTarget - w);
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? Dir.Right : Dir.Left;
        else return d.y > 0 ? Dir.Up : Dir.Down;
    }

    Vector2Int ToDelta(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return new Vector2Int(0, +1);
            case Dir.Left: return new Vector2Int(-1, 0);
            case Dir.Down: return new Vector2Int(0, -1);
            case Dir.Right: return new Vector2Int(+1, 0);
        }
        return Vector2Int.zero;
    }

    Dir Opposite(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Dir.Down;
            case Dir.Down: return Dir.Up;
            case Dir.Left: return Dir.Right;
            case Dir.Right: return Dir.Left;
        }
        return Dir.None;
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

    int ScareManagerPhase()
    {
        var s = FindObjectOfType<ScareManager>();
        if (!s) return 0;
        switch (s.CurrentPhase)
        {
            case ScareManager.Phase.Scared: return 1;
            case ScareManager.Phase.Recovering: return 2;
            default: return 0;
        }
    }

    void ReviveAccordingToPhase()
    {
        int phase = ScareManagerPhase();
        if (phase == 1) SetScared();
        else if (phase == 2) SetRecovering();
        else SetNormal();
    }

    void TryResumeMusic()
    {
        var all = FindObjectsOfType<GhostController>();
        bool anyDead = false;
        for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].IsDead) { anyDead = true; break; }
        if (anyDead) return;

        var bgm = FindObjectOfType<BgmPlayer>();
        if (!bgm) return;
        int phase = ScareManagerPhase();
        if (phase == 0) bgm.PlayNormalLoop();
    }

    void UpdateAnimatorDir(Dir d)
    {
        if (!animator) return;
        int v = (d == Dir.Down) ? 0 : (d == Dir.Left) ? 1 : (d == Dir.Up) ? 2 : 3;
        animator.SetInteger(hDir, v);
    }

    void ApplyAnimator()
    {
        if (!animator) return;
        animator.SetBool(hDead, mode == Mode.Dead);
        animator.SetBool(hScared, mode == Mode.Scared || mode == Mode.Recovering);
        animator.SetBool(hRecov, mode == Mode.Recovering);
    }

    void ResetGrid(Vector3 world)
    {
        gridPos = WorldToGrid(world);
        fromWorld = toWorld = GridToWorld(gridPos);
        t = 1f;
    }

    public bool IsDead => mode == Mode.Dead;
    public bool IsScared => mode == Mode.Scared;
    public bool IsRecovering => mode == Mode.Recovering;

    public void SetNormal() { mode = Mode.Normal; if (col) col.enabled = true; ApplyAnimator(); ResetGrid(transform.position); }
    public void SetScared() { mode = Mode.Scared; if (col) col.enabled = true; ApplyAnimator(); ResetGrid(transform.position); }
    public void SetRecovering() { mode = Mode.Recovering; if (col) col.enabled = true; ApplyAnimator(); ResetGrid(transform.position); }
    public void SetDead() { mode = Mode.Dead; if (col) col.enabled = false; ApplyAnimator(); fromWorld = toWorld = transform.position; t = 1f; hasExitedHouse = false; }

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
