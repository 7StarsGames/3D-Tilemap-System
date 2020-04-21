#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TilemapSystem3D
{
    [CreateAssetMenu(fileName = "New Tile Palette", menuName = "3D Tilemap System/ Tile Palette")]
    public class TilePalette : ScriptableObject
    {
        /// <summary>
        /// The tiles resolution.
        /// </summary>
        public int tileSize = 32;

        /// <summary>
        /// The atlas texture to convert to tiles.
        /// </summary>
        public Texture2D atlasTexture;

        /// <summary>
        /// The offset between each tile in the atlas texture.
        /// </summary>
        public int gridOffset = 0;

        /// <summary>
        /// The number of tiles in the atlas texture that you want to extract and convert to tiles.
        /// </summary>
        public int extractNumber = 255;

        /// <summary>
        /// The number of tiles inside this palette.
        /// </summary>
        public int tilesCount = 0;


        // Editor only
        #if UNITY_EDITOR
        /// <summary>
        /// Array that stores all the temporary tiles inside this palette.
        /// </summary>
        public Texture2D[] temporaryTileTextureArray = new Texture2D[256];

        /// <summary>
        /// Editor warning messages.
        /// </summary>
        private readonly string[] m_warning = new[]
        {
            "<b>3D Tilemap System:</b> There is no texture attached to the <b>Atlas Texture</b> field. Please add a texture and try again!",
            "<b>3D Tilemap System:</b> The atlas texture is not readable. Please, check the <b>Read/Write Enabled</b> option in the texture import settings to be able to perform the conversion.",
            "<b>3D Tilemap System:</b> Action aborted! The tile Pack is already full."
        };

        /// <summary>
        /// Called by the custom reset button.
        /// </summary>
        public void ResetAsset()
        {
            DeleteTiles();
            tileSize = 32;
            gridOffset = 0;
            tilesCount = 0;
            atlasTexture = null;
        }

        /// <summary>
        /// Delete all the temporary tile inside this palette.
        /// </summary>
        public void DeleteTiles()
        {
            for (int i = 0; i < 256; i++)
            {
                if (!temporaryTileTextureArray[i]) continue;
                
                DestroyImmediate(temporaryTileTextureArray[i], true);
            }
            
            tilesCount = 0;
            
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                EditorGUIUtility.PingObject(this);
            }
        }

        /// <summary>
        /// Extracts all the tiles of the atlas texture and save inside this pallete as .asset file.
        /// </summary>
        public void ConvertAtlasToTiles()
        {
            if (!atlasTexture)
            {
                Debug.LogWarning(m_warning[0]);
                return;
            }
            
            var texturePath = AssetDatabase.GetAssetPath(atlasTexture);
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if(!textureImporter.isReadable)
            {
                Debug.LogWarning(m_warning[1]);
                return;
            }
    
            bool isFree = false;
            for (int i = 0; i < 256; i++)
            {
                if (temporaryTileTextureArray[i]) continue;
                isFree = true;
            }
    
            if (!isFree)
            {
                Debug.LogWarning(m_warning[2]);
                return;
            }
            
            // Calculates the amount of tiles in the atlas texture
            var horizontalColumns = atlasTexture.width / (tileSize + gridOffset);
            var verticalColumns = atlasTexture.height / (tileSize + gridOffset);
            
            // The tile index currently being scanned
            var tileIndex = 0;
            
            // Scans each tile of the atlas texture
            for (var y = verticalColumns -1; y >= 0; y--)
            {
                for (var x = 0; x < horizontalColumns; x++)
                {
                    if (tileIndex > 255 || tileIndex >= extractNumber) break;
                    while (temporaryTileTextureArray[tileIndex] && tileIndex < 255)
                    {
                        tileIndex++;
                    }
    
                    if (temporaryTileTextureArray[tileIndex]) continue;
    
                    // Get the color of the current tile from the atlas texture
                    var tileColor = atlasTexture.GetPixels(x * (tileSize + gridOffset) + gridOffset, y * (tileSize + gridOffset) + gridOffset, tileSize, tileSize);
                    
                    // Create the tile texture
                    temporaryTileTextureArray[tileIndex] = new Texture2D(tileSize, tileSize, TextureFormat.ARGB32, false, true)
                    {
                        name = "Tile" + tileIndex,
                        filterMode = FilterMode.Point,
                        alphaIsTransparency = true,
                        wrapMode = TextureWrapMode.Repeat,
                        hideFlags = HideFlags.DontSaveInBuild
                    };
                    
                    // Paste the tile color to new tile texture
                    temporaryTileTextureArray[tileIndex].SetPixels(tileColor);
                    temporaryTileTextureArray[tileIndex].Apply();
                    
                    // Save the new tile texture into this palette as .asset file
                    AssetDatabase.AddObjectToAsset(temporaryTileTextureArray[tileIndex], this);
                    
                    // Go to the next tile
                    tileIndex++;
                    tilesCount++;
                }
            }
            
            // Refresh the project window
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            EditorGUIUtility.PingObject(this);
        }
        #endif
    }
}