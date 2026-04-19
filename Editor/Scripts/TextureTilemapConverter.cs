using UnityEngine;

namespace CrossingLears.Editor
{
    public class TextureTilemapConverter : ScriptableObject
    {
        public string originalTexturePath;
        public Vector2Int originalTextureDimensions;
        public Vector2Int tileSize;
        public int[] spriteIndices = new int[49];
    }
}
