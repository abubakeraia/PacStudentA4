using UnityEngine;
using UnityEngine.Tilemaps;

public class PelletCollector : MonoBehaviour
{
    [Header("Refs")]
    public Transform pac;
    public Tilemap[] pelletTilemaps;
    public HUDController hud;
    public GameManager gm;

    [Header("Settings")]
    public int pelletScore = 10;
    public AudioClip pelletSfx;
    [Range(0f, 1f)] public float pelletSfxVolume = 0.25f;
    public ParticleSystem pelletVfxPrefab;

    Tilemap lastTm;
    Vector3Int lastCell;

    void Start()
    {
        if (!pac) pac = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!gm) gm = FindObjectOfType<GameManager>();
        lastTm = null;
        lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    void Update()
    {
        if (!pac || pelletTilemaps == null || pelletTilemaps.Length == 0) return;

        foreach (var tm in pelletTilemaps)
        {
            if (!tm || !tm.gameObject.activeInHierarchy) continue;

            var cell = tm.WorldToCell(pac.position);

            if (tm == lastTm && cell == lastCell) continue;

            if (tm.HasTile(cell))
            {
                tm.SetTile(cell, null);
                hud?.AddScore(pelletScore);
                gm?.OnPelletEaten();

                var p = tm.GetCellCenterWorld(cell);
                if (pelletSfx) AudioSource.PlayClipAtPoint(pelletSfx, p, pelletSfxVolume);
                if (pelletVfxPrefab)
                {
                    var vfx = Instantiate(pelletVfxPrefab, p, Quaternion.identity);
                    var main = vfx.main; main.stopAction = ParticleSystemStopAction.Destroy;
                    vfx.Play();
                }

                lastTm = tm;
                lastCell = cell;
                break;
            }

            lastTm = tm;
            lastCell = cell;
        }
    }
}
