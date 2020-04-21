using System;

namespace UnityEngine.TilemapSystem3D
{
    /// <summary>
    /// Used to set the Render Pipeline used by the current project.
    /// </summary>
    public enum TilemapSystemPipeline
    {
        Legacy,
        Universal
    }
    
    /// <summary>
    /// 
    /// </summary>
    public enum TilemapSystemPaintingMode
    {
        Default,
        AutoTiles,
        RandomTilePainting,
        TileBrushes
    }
    
    /// <summary>
    /// Used by the auto tile list to store the layout for the 8-Bitmasking computations.
    /// </summary>
    [Serializable] public sealed class AutoTileLayout
    {
        public int[] id = new int[47];
        
        // Not included in build
        #if UNITY_EDITOR
        public bool[] isPicked = new bool[47];
        public string name;
        #endif
    }
    
    /// <summary>
    /// Stores some important settings of the Tilemap System component.
    /// Used in the TilemapData.asset to check if the user changed some settings in the Inspector.
    /// </summary>
    [Serializable] public struct TilemapSettings
    {
        public int layersCount;
        public int[] tilesCount;
        public TilePalette[] tilePalette;
        public int tilemapWidth;
        public int tilemapHeight;
        public int tilesetSize;

        public TilemapSettings(int layersCount, int[] tilesCount, TilePalette[] tilePalette, int tilemapWidth, int tilemapHeight, int tilesetSize)
        {
            this.layersCount = layersCount;
            this.tilesCount = tilesCount;
            this.tilePalette = tilePalette;
            this.tilemapWidth = tilemapWidth;
            this.tilemapHeight = tilemapHeight;
            this.tilesetSize = tilesetSize;
        }
    }
    
    /// <summary>
    /// Stores the tilemap data of a layer.
    /// Used in the TilemapData.asset to perform the copy and paste feature.
    /// </summary>
    [Serializable] public sealed class LayerClipboard
    {
        public int layerIndex;
        public Texture2D tilemapSlice;

        public LayerClipboard(int layerIndex, Texture2D tilemapSlice)
        {
            this.layerIndex = layerIndex;
            this.tilemapSlice = tilemapSlice;
        }
    }
}