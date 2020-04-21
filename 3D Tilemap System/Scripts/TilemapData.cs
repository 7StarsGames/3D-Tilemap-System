namespace UnityEngine.TilemapSystem3D
{
    public class TilemapData : ScriptableObject
    {
        [HideInInspector] public Texture3D tilemapTexture;
        [HideInInspector] public Texture3D tilesetTexture;
        [HideInInspector] public Texture2D layerArrayTexture;
        [HideInInspector] public int[] autoTileArrayMap;
        
        // Editor only
        #if UNITY_EDITOR
        [HideInInspector] public TilemapSettings settingsState;
        [HideInInspector] public LayerClipboard layerClipboard;
        public float[] layerIntensity;
        #endif
    }
}