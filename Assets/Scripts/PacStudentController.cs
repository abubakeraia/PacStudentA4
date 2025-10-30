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

    [Header("Start Override")]
    public bool useExplicitStartWorld = false;
    public Vector3 explicitStartWorld;

    [Header("Animator")]
    public Animator animator;

    [Header("Debug (read-only)")]
    public Dir lastInput = Dir.None;
    public Dir currentInput = Dir.None;

    [Header("Bump FX")]
    public AudioSource sfxSource;
    public AudioClip wallBumpClip;
    public ParticleSystem bumpVfxPrefab;
    public float bumpCooldown = 0.05f;

    float lastBumpTime = -999f;

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
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Dir.Up;
        else if (Input.GetKeyDown(KeyCode.A)) lastInput = Dir.Left;
        else if (Input.GetKeyDown(KeyCode.S)) lastInput = Dir.Down;
        else if (Input.GetKeyDown(KeyCode.D)) lastInput = Dir.Right;

        if (!IsMoving)
        {
            if (!TryStartMove(lastInput))
                TryStartMove(currentInput);
        }
        else
        {
            float worldSpeed = speedCellsPerSecond * cellSize;
            float dist = Vector3.Distance(fromWorld, toWorld);
            float dur = Mathf.Max(0.0001f, dist / worldSpeed);
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(fromWorld, toWorld, Mathf.Clamp01(t));
        }

        if (!IsMoving && transform.position != toWorld)
            transform.position = toWorld;
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
    }


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
}
