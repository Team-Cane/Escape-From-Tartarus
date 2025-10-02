using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject chunkPrefab;
    [SerializeField] int startingChunksAmount = 15;
    [SerializeField] Transform chunkParent;
    [SerializeField] float chunkLength = 48f;  
    [SerializeField] float moveSpeed = 8f;

    List<GameObject> chunks = new List<GameObject>();

    void Start()
    {
        spawnStartingChunks();
    }

    void Update()
    {
        moveChunks();
    }

    void spawnStartingChunks()
    {
        for (int i = 0; i < startingChunksAmount; i++)
        {
            SpawnChunk();
        }
    }

    private void SpawnChunk()
    {
        Vector3 spawnPos;

        if (chunks.Count == 0)
        {
            // First chunk at generator's position
            spawnPos = transform.position;
        }
        else
        {
            // Place new chunk so its StartAnchor aligns to the last chunk's EndAnchor
            Transform lastChunk = chunks[chunks.Count - 1].transform;
            Transform lastEnd = lastChunk.Find("EndAnchor");

            if (lastEnd != null)
            {
                spawnPos = lastEnd.position;
            }
            else
            {
                // Fallback if anchors are missing on the last chunk
                spawnPos = lastChunk.position + Vector3.forward * chunkLength; // old spacing
            }
        }

        GameObject newChunk = Instantiate(chunkPrefab, spawnPos, Quaternion.identity, chunkParent);

        // --- CRITICAL ALIGNMENT STEP ---
        // Move the new chunk so that its StartAnchor EXACTLY matches spawnPos (last EndAnchor)
        Transform startAnchor = newChunk.transform.Find("StartAnchor");
        if (startAnchor != null)
        {
            Vector3 delta = startAnchor.position - newChunk.transform.position;
            newChunk.transform.position = spawnPos - delta;
        }
        // --------------------------------

        // Obstacles per chunk (your existing call)
        ChunkObstacleSpawner spawner = newChunk.GetComponent<ChunkObstacleSpawner>();
        if (spawner != null)
        {
            spawner.Initialize(4.55f, chunks.Count);
        }

        chunks.Add(newChunk);
    }

    void moveChunks()
    {
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = chunks[i];
            chunk.transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);

            // Despawn when the chunk is fully behind the camera:
            // use EndAnchor if present; otherwise fall back to old check.
            Transform endAnchor = chunk.transform.Find("EndAnchor");
            float endZ = endAnchor != null ? endAnchor.position.z : (chunk.transform.position.z);

            // small buffer so we don't despawn too early
            float despawnZ = Camera.main.transform.position.z - 2f;

            if (endZ <= despawnZ)
            {
                chunks.RemoveAt(i);
                Destroy(chunk);
                SpawnChunk();
            }
        }
    }
}
