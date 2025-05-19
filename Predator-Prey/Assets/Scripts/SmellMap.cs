using UnityEngine;
using System.Collections.Generic;

public class SmellMap {
    private float[,] smellGrid;
    private int gridSizeX, gridSizeZ;
    private float cellSize;
    private float decayRate;
    private Vector3 origin;

    public SmellMap(Bounds bounds, float cellSize, float decayRate) {
        this.cellSize = cellSize;
        this.decayRate = decayRate;
        this.origin = new Vector3(bounds.min.x, 0f, bounds.min.z);

        gridSizeX = Mathf.CeilToInt(bounds.size.x / cellSize);
        gridSizeZ = Mathf.CeilToInt(bounds.size.z / cellSize);

        smellGrid = new float[gridSizeX, gridSizeZ];
        for (int x = 0; x < gridSizeX; x++)
            for (int z = 0; z < gridSizeZ; z++)
                smellGrid[x, z] = 0f;
    }

    public void AddSmell(Vector3 position, float intensity) {
        var index = WorldToGrid(position);
        if (IsValidIndex(index))
            smellGrid[index.x, index.y] = intensity;
    }

    public float GetSmell(Vector3 position) {
        var index = WorldToGrid(position);
        return IsValidIndex(index) ? smellGrid[index.x, index.y] : 0f;
    }

    public List<float> GetSmellRadius(Vector3 position, float radius) {
        List<float> smellValues = new List<float>();

        Vector2Int center = WorldToGrid(position);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        float radiusSqr = radius * radius;

        for (int dx = -cellRadius; dx <= cellRadius; dx++) {
            for (int dz = -cellRadius; dz <= cellRadius; dz++) {
                int x = center.x + dx;
                int z = center.y + dz;

                if (IsValidIndex(new Vector2Int(x, z))) {
                    smellValues.Add(smellGrid[x, z]);
                }
                else {
                    smellValues.Add(0f);
                }
            }
        }
        return smellValues;
    }

    public void DecaySmell() {
        for (int x = 0; x < gridSizeX; x++)
        for (int z = 0; z < gridSizeZ; z++)
            smellGrid[x, z] *= decayRate;
    }

    private Vector2Int WorldToGrid(Vector3 pos) {
        Vector3 localPos = pos - origin;
        return new Vector2Int(
            Mathf.FloorToInt(localPos.x / cellSize),
            Mathf.FloorToInt(localPos.z / cellSize)
        );
    }

    private bool IsValidIndex(Vector2Int index) {
        return index.x >= 0 && index.x < gridSizeX &&
               index.y >= 0 && index.y < gridSizeZ;
    }
}
