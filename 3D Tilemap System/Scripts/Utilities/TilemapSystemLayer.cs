using System;
using System.Collections.Generic;

namespace UnityEngine.TilemapSystem3D
{
    /// <summary>
    /// Class that stores all the necessary data for painting each layer.
    /// </summary>
    [Serializable]
    public sealed class TilemapSystemLayer
    {
        /// <summary>
        /// The tile palette used to paint on this layer.
        /// </summary>
        public TilePalette tilePalette;

        /// <summary>
        /// The auto tile layout list of this layer.
        /// </summary>
        public List<AutoTileLayout> autoTileList = new List<AutoTileLayout>();

        /// <summary>
        /// The alpha intensity of this layer.
        /// </summary>
        public float intensity = 1.0f;

        // TODO: Add support for multiples auto tile array maps and store it in the TilemapData.asset
        // -1 = regular tile
        //  0 = tile painted using the first auto tile layout
        //  1 = tile painted using the second auto tile layout
        //  2 = tile painted using the third auto tile layout
        //  ...
        //public int[] autoTileArrayMap;

        /// <summary>
        /// Array map used by the auto tile feature.
        /// </summary>
        public bool[] autoTileMapArray;

        // Editor only
        #if UNITY_EDITOR
        /// <summary>
        /// Stores the layer name. Only used in the editor.
        /// </summary>
        public string layerName;

        /// <summary>
        /// The layer icon texture. Only used in the editor.
        /// </summary>
        public Texture2D iconTexture;

        /// <summary>
        /// The layout index from the auto tile list that should be used for painting the auto tiles.
        /// Only used in the editor.
        /// </summary>
        public int layoutIndex = 0;
        #endif
    }
}