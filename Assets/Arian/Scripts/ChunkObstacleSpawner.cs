using System.Collections.Generic;
using UnityEngine;

public class ChunkObstacleSpawner : MonoBehaviour
{
    [Header("Setup")]
    public List<GameObject> obstaclePrefabs;    // drag your 12 obstacles here on the chunk prefab
    public float laneOffset = 4.55f;            // distance between lanes (L <-> M <-> R)
    public float zStartOffset = 2f;             // first row Z offset from chunk start
    public float rowSpacingZ = 8f;              // Z distance between rows
    public int rowsPerChunk = 2;                // rows to attempt per chunk

    [Tooltip("Optional. If set/found, this defines the middle lane X. If null, spawns relative to chunk root.")]
    [SerializeField] Transform laneCenter;

    [Header("Randomness / Difficulty")]
    [Range(0, 1f)] public float emptyRowChance = 0.15f;   // chance to skip a row entirely (breather)
    [Range(0, 1f)] public float twoWideChance = 0.35f;    // chance to try a 2-lane obstacle
    [Range(0, 1f)] public float extraSingleChance = 0.25f;// chance to add a second single in same row

    [Header("Pickups")]
    public GameObject coinPrefab;                         // single coin (trigger)
    public List<GameObject> powerupPrefabs;               // e.g., Shield/Magnet/ScoreBoost (triggers)
    [Range(0, 1f)] public float coinRowChance = 0.55f;    // chance to place coins / start a trail
    public Vector2Int coinTrailRows = new Vector2Int(2, 4); // consecutive rows for a coin trail
    [Range(0, 1f)] public float powerupChance = 0.15f;    // per-row chance for a power-up
    public int powerupRowGap = 3;                         // min rows between power-ups

    void Awake()
    {
        // Find LaneCenter automatically if not assigned
        if (!laneCenter)
        {
            var t = transform.Find("LaneCenter");
            if (t) laneCenter = t;
        }
    }

    // Called by LevelGenerator right after the chunk is instantiated
    public void Initialize(float laneOffsetFromPlayer, int difficultyLevel = 0)
    {
        laneOffset = laneOffsetFromPlayer;

        // Simple difficulty ramp by chunk count (tweak as desired)
        twoWideChance = Mathf.Clamp01(twoWideChance + 0.02f * difficultyLevel);
        emptyRowChance = Mathf.Clamp01(emptyRowChance - 0.01f * difficultyLevel);

        SpawnRows();
    }

    // ---------------------------
    // Spawning (with fairness + pickups)
    // ---------------------------

    void SpawnRows()
    {
        float baseZ = transform.position.z + zStartOffset;

        // Track the last row’s blocked lanes and streaks to avoid “walls”
        bool[] lastRowBlocked = new bool[3];  // [L, M, R] lane blocked in previous row
        int[] blockStreak = new int[3];       // consecutive rows a lane has been blocked
        int twoWideCooldown = 0;              // prevents back-to-back 2-wide walls

        // Coin trail state
        int coinTrailLane = -1;               // -1 = no active trail
        int coinTrailRowsLeft = 0;

        int powerupCooldownRows = 0;

        for (int r = 0; r < rowsPerChunk; r++)
        {
            float rowZ = baseZ + r * rowSpacingZ;

            // Optional “breather” row
            if (Random.value < emptyRowChance)
            {
                // Reset lane pressure
                for (int i = 0; i < 3; i++) { blockStreak[i] = 0; lastRowBlocked[i] = false; }
                twoWideCooldown = Mathf.Max(0, twoWideCooldown - 1);

                // Let a coin trail run through empty rows if active
                if (coinTrailLane >= 0)
                {
                    TrySpawnCoinAtLane(coinTrailLane, rowZ, null);
                    coinTrailRowsLeft--;
                    if (coinTrailRowsLeft <= 0) coinTrailLane = -1;
                }

                if (--powerupCooldownRows < 0) powerupCooldownRows = 0;
                continue;
            }

            bool[] laneBlocked = new bool[3]; // 0=L, 1=M, 2=R

            // ---- 2-wide placement (with cooldown/fairness) ----
            bool placedTwoWide = false;
            if (twoWideCooldown == 0 && Random.value < twoWideChance)
            {
                placedTwoWide = TryPlaceTwoWideSafe(laneBlocked, lastRowBlocked, blockStreak, rowZ);
                if (placedTwoWide) twoWideCooldown = 1; // prevent back-to-back 2-wide
            }
            else
            {
                twoWideCooldown = Mathf.Max(0, twoWideCooldown - 1);
            }

            // ---- Single-lane obstacles (max 2 per row) ----
            int singlesPlaced = 0;
            if (TryPlaceSingleSafe(laneBlocked, lastRowBlocked, blockStreak, rowZ)) singlesPlaced++;
            if (singlesPlaced < 2 && Random.value < extraSingleChance)
            {
                if (TryPlaceSingleSafe(laneBlocked, lastRowBlocked, blockStreak, rowZ)) singlesPlaced++;
            }

            // ---- Guarantees: never all 3 blocked in the same row ----
            if (laneBlocked[0] && laneBlocked[1] && laneBlocked[2])
            {
                // free a random lane
                int laneToFree = Random.Range(0, 3);
                laneBlocked[laneToFree] = false;
            }

            // If same lanes were blocked last row too, force-open the worst streak
            if ((laneBlocked[0] && lastRowBlocked[0]) &&
                (laneBlocked[1] && lastRowBlocked[1]) &&
                (laneBlocked[2] && lastRowBlocked[2]))
            {
                int worst = ArgMax(blockStreak);
                laneBlocked[worst] = false;
            }

            // ---- Pickups (only in free lanes) ----

            // Coins: maintain or start a trail if possible
            if (coinPrefab)
            {
                if (coinTrailLane >= 0)
                {
                    if (!laneBlocked[coinTrailLane])
                    {
                        TrySpawnCoinAtLane(coinTrailLane, rowZ, laneBlocked);
                        coinTrailRowsLeft--;
                        if (coinTrailRowsLeft <= 0) coinTrailLane = -1;
                    }
                    else
                    {
                        // trail interrupted by obstacle
                        coinTrailLane = -1;
                    }
                }
                else
                {
                    if (Random.value < coinRowChance)
                    {
                        int lane = PickBestFreeLane(laneBlocked, blockStreak);
                        if (lane >= 0)
                        {
                            TrySpawnCoinAtLane(lane, rowZ, laneBlocked);
                            coinTrailLane = lane;
                            coinTrailRowsLeft = Random.Range(coinTrailRows.x, coinTrailRows.y + 1);
                        }
                    }
                    else
                    {
                        int lane = PickBestFreeLane(laneBlocked, blockStreak);
                        if (lane >= 0) TrySpawnCoinAtLane(lane, rowZ, laneBlocked);
                    }
                }
            }

            // Power-ups: rare, in free lanes, with a cooldown gap
            if (powerupCooldownRows == 0 && powerupPrefabs != null && powerupPrefabs.Count > 0)
            {
                if (Random.value < powerupChance)
                {
                    int lane = PickBestFreeLane(laneBlocked, blockStreak);
                    if (lane >= 0)
                    {
                        SpawnPowerupAtLane(lane, rowZ);
                        powerupCooldownRows = powerupRowGap;
                    }
                }
            }
            if (powerupCooldownRows > 0) powerupCooldownRows--;

            // ---- Update lane pressure for next row ----
            for (int i = 0; i < 3; i++)
            {
                blockStreak[i] = laneBlocked[i] ? Mathf.Min(blockStreak[i] + 1, 3) : 0;
                lastRowBlocked[i] = laneBlocked[i];
            }
        }
    }

    // ---------------------------
    // Lane helpers & placement
    // ---------------------------

    float LaneX(int laneIndex)
    {
        float cx = laneCenter ? laneCenter.position.x : transform.position.x;
        // 0 = Left, 1 = Middle, 2 = Right
        return laneIndex == 0 ? cx - laneOffset : (laneIndex == 1 ? cx : cx + laneOffset);
    }

    bool TryPlaceTwoWideSafe(bool[] laneBlocked, bool[] lastRowBlocked, int[] blockStreak, float z)
    {
        // candidates: LM(start 0) or MR(start 1)
        var starts = new List<int>();
        if (!laneBlocked[0] && !laneBlocked[1]) starts.Add(0);
        if (!laneBlocked[1] && !laneBlocked[2]) starts.Add(1);
        if (starts.Count == 0) return false;

        // Prefer the side that reduces streak pressure
        if (starts.Count == 2)
        {
            int lmPressure = (lastRowBlocked[0] ? 1 : 0) + (lastRowBlocked[1] ? 1 : 0) + blockStreak[0] + blockStreak[1];
            int mrPressure = (lastRowBlocked[1] ? 1 : 0) + (lastRowBlocked[2] ? 1 : 0) + blockStreak[1] + blockStreak[2];
            if (lmPressure > mrPressure) starts.Remove(0);
            else if (mrPressure > lmPressure) starts.Remove(1);
        }

        int start = starts[Random.Range(0, starts.Count)];

        // pick a matching 2-wide prefab
        var options = new List<GameObject>();
        foreach (var p in obstaclePrefabs)
        {
            var meta = p.GetComponent<ObstacleMeta>();
            if (!meta || meta.laneWidth != 2) continue; // requires your ObstacleMeta (laneWidth, LM/MR flags) :contentReference[oaicite:1]{index=1}
            if (start == 0 && meta.allowLeftMiddle) options.Add(p);
            if (start == 1 && meta.allowMiddleRight) options.Add(p);
        }
        if (options.Count == 0) return false;

        var pick = options[Random.Range(0, options.Count)];

        float xA = LaneX(start == 0 ? 0 : 1);
        float xB = LaneX(start == 0 ? 1 : 2);
        Vector3 pos = new Vector3(0.5f * (xA + xB), transform.position.y, z);
        Instantiate(pick, pos, Quaternion.identity, transform);

        // mark blocked lanes
        if (start == 0) { laneBlocked[0] = true; laneBlocked[1] = true; }
        else { laneBlocked[1] = true; laneBlocked[2] = true; }

        return true;
    }

    bool TryPlaceSingleSafe(bool[] laneBlocked, bool[] lastRowBlocked, int[] blockStreak, float z)
    {
        // lanes still free this row
        var free = new List<int>();
        for (int i = 0; i < 3; i++) if (!laneBlocked[i]) free.Add(i);
        if (free.Count == 0) return false;

        // prefer lanes with smaller streaks (keeps a path open across rows)
        free.Sort((a, b) => blockStreak[a].CompareTo(blockStreak[b]));

        foreach (int lane in free)
        {
            // prevent filling the 3rd lane with a single
            int blockedCount = (laneBlocked[0] ? 1 : 0) + (laneBlocked[1] ? 1 : 0) + (laneBlocked[2] ? 1 : 0);
            if (blockedCount >= 2) break;

            // pick a single-lane prefab allowed in this lane
            var options = new List<GameObject>();
            foreach (var p in obstaclePrefabs)
            {
                var meta = p.GetComponent<ObstacleMeta>();
                if (!meta || meta.laneWidth != 1) continue;
                if (lane == 0 && meta.allowLeft) options.Add(p);
                if (lane == 1 && meta.allowMiddle) options.Add(p);
                if (lane == 2 && meta.allowRight) options.Add(p);
            }
            if (options.Count == 0) continue;

            var pick = options[Random.Range(0, options.Count)];

            float x = LaneX(lane);
            Vector3 pos = new Vector3(x, transform.position.y, z);
            Instantiate(pick, pos, Quaternion.identity, transform);

            laneBlocked[lane] = true;
            return true;
        }

        return false;
    }

    // ---------------------------
    // Pickups
    // ---------------------------

    int PickBestFreeLane(bool[] laneBlocked, int[] blockStreak)
    {
        var free = new List<int>();
        for (int i = 0; i < 3; i++) if (!laneBlocked[i]) free.Add(i);
        if (free.Count == 0) return -1;

        free.Sort((a, b) => blockStreak[a].CompareTo(blockStreak[b]));
        return free[0];
    }

    void TrySpawnCoinAtLane(int lane, float z, bool[] laneBlockedOrNull)
    {
        if (!coinPrefab) return;
        if (laneBlockedOrNull != null && laneBlockedOrNull[lane]) return;

        float x = LaneX(lane);
        Vector3 pos = new Vector3(x, transform.position.y + 0.5f, z);
        Instantiate(coinPrefab, pos, Quaternion.identity, transform);
    }

    void SpawnPowerupAtLane(int lane, float z)
    {
        if (powerupPrefabs == null || powerupPrefabs.Count == 0) return;

        var pick = powerupPrefabs[Random.Range(0, powerupPrefabs.Count)];
        float x = LaneX(lane);
        Vector3 pos = new Vector3(x, transform.position.y + 0.75f, z);
        Instantiate(pick, pos, Quaternion.identity, transform);
    }

    int ArgMax(int[] a) { int k = 0; for (int i = 1; i < a.Length; i++) if (a[i] > a[k]) k = i; return k; }
}
