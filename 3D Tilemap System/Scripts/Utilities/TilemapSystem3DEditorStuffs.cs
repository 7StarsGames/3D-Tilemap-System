using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This script file contains only the editor stuffs of the 'TilemapSystem3D' class.
/// </summary>
namespace UnityEngine.TilemapSystem3D
{
    public partial class TilemapSystem3D : MonoBehaviour
    {
        // Editor only
        #if UNITY_EDITOR
        /// <summary>
        /// The texture used to apply the default color to the mesh while a tileset does not yet exist.
        /// </summary>
        public Texture2D defaultColorTexture;

        /// <summary>
        /// The current tilemap layer selected for painting.
        /// </summary>
        [NonSerialized] public int layerIndex = 0;

        /// <summary>
        /// The current tile selected for painting.
        /// </summary>
        [NonSerialized] public int tileIndex = 0;

        /// <summary>
        /// The current tool selected for painting.
        /// </summary>
        [NonSerialized] public int toolIndex = 0;

        /// <summary>
        /// Select the feature used to paint the tiles.
        /// </summary>
        public TilemapSystemPaintingMode paintingMode = TilemapSystemPaintingMode.Default;

        // Controls the foldout header groups
        public bool showReferencesGroup = true;
        public bool showSettingsGroup = true;
        public bool showDataGroup = true;
        public bool showPantingGroup = true;
        public bool showAutoTileGroup = true;
        public bool showRandomTilePaintingGroup = false;
        public bool showRandomizeTileGroup = false;
        public bool showTileBrushesGroup = false;
        public bool showAboutGroup = true;

        /// <summary>
        /// Creates the textures and material used by the Tilemap System.
        /// </summary>
        public void GenerateTilemapData()
        {
            // If the tilemap data does not yet exist
            if (!tilemapData)
            {
                // Displays the "save file" dialog and returns the selected path name
                var savePath = EditorUtility.SaveFilePanel
                (
                    "Select the folder directory were the tilemap data should be saved",
                    Application.dataPath,
                    "Tilemap Data",
                    "asset"
                );

                // Checks if the path name is valid, otherwise returns
                if (savePath.Length <= 0) return;

                // Edit the path to be relative to the project folder
                savePath = savePath.Replace(Application.dataPath, "Assets");

                // Delete the previous file
                if (System.IO.File.Exists(savePath)) AssetDatabase.DeleteAsset(savePath);

                // Create the tilemap data
                tilemapData = ScriptableObject.CreateInstance<TilemapData>();

                // Save the tilemap data as .asset file
                AssetDatabase.CreateAsset(tilemapData, savePath);

                // Create the 3D Tilemap texture
                CreateTilemapTexture();

                // Create the 3D Tileset texture
                CreateTilesetTexture();

                // Create the layer data array texture
                CreateLayerDataArray();

                // Create the render material
                switch (renderPipeline)
                {
                    case TilemapSystemPipeline.Legacy:
                        CreateRenderMaterial(legacyShader);
                        break;

                    case TilemapSystemPipeline.Universal:
                        CreateRenderMaterial(universalShader);
                        break;
                }

                // Update material
                UpdateRenderMaterial();

                // Ping the tilemap data to refresh the editor.
                EditorGUIUtility.PingObject(tilemapData);
                return;
            }

            // Update the tilemap texture
            UpdateTilemapTexture();

            // Update the tileset texture
            UpdateTilesetTexture();

            // Update the layer data array texture
            UpdateLayerDataArray();

            // Update material
            UpdateRenderMaterial();

            // Whenever a new texture is created, its old version is deleted and the new texture is referenced to the variable.
            // But if you don't save the scene, the new texture reference will be lost and when you open the scene again, the
            // variable will try to access the old texture that no longer exists. So the solution is to use the TilemapData to
            // store the new references and load it again at Start().
            tilemapData.tilemapTexture = tilemapTexture;
            tilemapData.tilesetTexture = tilesetTexture;
            tilemapData.layerArrayTexture = layerArrayTexture;

            // Ping the tilemap data to refresh the editor
            EditorGUIUtility.PingObject(tilemapData);
        }

        /// <summary>
        /// Creates a black 3D Tilemap texture.
        /// </summary>
        private void CreateTilemapTexture()
        {
            int width = (int)gridMapSize.x;
            int height = (int)gridMapSize.y;
            int depth = tilemapLayerList.Count;

            // Starts the temporary colors full black.
            Color32[] tempColor = new Color32[width * height * depth];

            // Create the 3D tilemap texture
            tilemapTexture = new Texture3D(width, height, depth, TextureFormat.R8, false)
            {
                name = "Tilemap Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            // Apply the colors to the tilemap texture
            tilemapTexture.SetPixels32(tempColor);
            tilemapTexture.Apply();

            // Save the tilemap texture as .asset file
            AssetDatabase.AddObjectToAsset(tilemapTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tilemapTexture));
        }

        /// <summary>
        /// Updates the 3D Tilemap texture when any important settings are changed.
        /// </summary>
        private void UpdateTilemapTexture()
        {
            int width = (int)gridMapSize.x;
            int height = (int)gridMapSize.y;
            int depth = tilemapLayerList.Count;

            // If the user changes the grid map size, the 3D Tilemap texture will need to be resized and
            // the map data for each layer will be lost.
            // So there is no alternative but to just create a new blank tilemap texture and return
            if (width != tilemapTexture.width || height != tilemapTexture.height)
            {
                DestroyImmediate(tilemapTexture, true);
                CreateTilemapTexture();
                return;
            }

            // Copy the current color of the tilemap texture
            Color32[] oldColor = tilemapTexture.GetPixels32();

            // Destroy the tilemap texture to create it again in the next step
            DestroyImmediate(tilemapTexture, true);

            // Recreate the tilemap texture with the new layer settings
            tilemapTexture = new Texture3D(width, height, depth, TextureFormat.R8, false)
            {
                name = "Tilemap Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            // Creates the new color array
            Color32[] newColor = new Color32[width * height * depth];

            // Paste the old color to the new tilemap texture
            if (newColor.Length < oldColor.Length)
            {
                for (int i = 0; i < newColor.Length; i++)
                {
                    newColor[i] = oldColor[i];
                }
            }

            if (newColor.Length >= oldColor.Length)
            {
                for (int i = 0; i < oldColor.Length; i++)
                {
                    newColor[i] = oldColor[i];
                }
            }

            // Apply the changes to the new tilemap texture
            tilemapTexture.SetPixels32(newColor);
            tilemapTexture.Apply();

            // Save the new tilemap texture as .asset file
            AssetDatabase.AddObjectToAsset(tilemapTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tilemapTexture));
        }

        /// <summary>
        /// Creates the 3D Tileset texture with a default color.
        /// </summary>
        private void CreateTilesetTexture()
        {
            int width = defaultColorTexture.width;
            int height = defaultColorTexture.height;
            int depth = 1;

            // Copy the default color texture to the temporary color array
            Color32[] tempColors = defaultColorTexture.GetPixels32();

            // Create the 3D tileset texture
            tilesetTexture = new Texture3D(width, height, depth, TextureFormat.RGBA32, false)
            {
                name = "Tileset Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0
            };

            // Apply the colors to the tileset texture
            tilesetTexture.SetPixels32(tempColors);
            tilesetTexture.Apply();

            // Save the tileset texture as .asset file
            AssetDatabase.AddObjectToAsset(tilesetTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tilesetTexture));
        }

        /// <summary>
        /// Updates the 3D Tileset texture when any important settings are changed.
        /// </summary>
        private void UpdateTilesetTexture()
        {
            // Get the tile palette of each layer
            TilePalette[] paletteList = new TilePalette[tilemapLayerList.Count];
            for (int i = 0; i < tilemapLayerList.Count; i++)
            {
                paletteList[i] = tilemapLayerList[i].tilePalette;
            }

            // Get the total number of tiles in all the palettes
            var tilesCount = 0;
            for (var i = 0; i < paletteList.Length; i++)
            {
                if (paletteList[i])
                {
                    tilesCount += paletteList[i].tilesCount;

                    // Return if the tile palette is empty
                    if (paletteList[i].tilesCount == 0)
                    {
                        Debug.LogWarning("<b>3D Tilemap System:</b> Can't pack the 3D Tileset texture because the Tile Palette field of the Layer" + i + " is empty. Please, add the tiles to the Tile Palette first and try to regenerate the Tilemap Data again.");
                        return;
                    }

                    // Return if the tiles size in the palette do not match with the 3D Tileset texture size
                    if (paletteList[i].tileSize != tilesetSize)
                    {
                        Debug.LogWarning("<b>3D Tilemap System:</b> Can't pack the 3D Tileset texture because the tiles size in the Tile Palette of the Layer" + i + " do not match with the 3D Tileset texture size.");
                        return;
                    }
                }
                else
                {
                    // Return if there is no tile palette attached to some layer
                    Debug.LogWarning("<b>3D Tilemap System:</b> Can't pack the 3D Tileset texture because the Tile Palette field of the Layer" + i + " is null. Please, attach a Tile Palette to it and try to regenerate the Tilemap Data again.");
                    return;
                }
            }

            // Delete the existing 3D Tileset texture before creating a new one
            DestroyImmediate(tilesetTexture, true);

            // Initialize the tileset texture
            tilesetTexture = new Texture3D(tilesetSize, tilesetSize, tilesCount + 1, TextureFormat.RGBA32, false)
            {
                name = "Tileset Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0
            };

            // Pack the tiles from paletteList[] to Texture3D
            Color[] tilesetColor = tilesetTexture.GetPixels();
            int layer = 0;
            int index = 0;
            int tileSize = tilesetTexture.width;

            for (int z = 0; z < tilesCount; z++)
            {
                var tileColor = paletteList[layer].temporaryTileTextureArray[index].GetPixels32();

                for (int y = 0; y < tileSize; y++)
                {
                    for (int x = 0; x < tileSize; x++)
                    {
                        tilesetColor[x + (y * tileSize) + (z * tileSize * tileSize)] = tileColor[x + (y * tileSize)];
                    }
                }

                index++;
                if (index >= paletteList[layer].tilesCount)
                {
                    index = 0;
                    layer++;
                }
            }

            // Apply the changes to the tileset texture
            tilesetTexture.SetPixels(tilesetColor);
            tilesetTexture.Apply();

            // Save the tileset texture as .asset file
            AssetDatabase.AddObjectToAsset(tilesetTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tilesetTexture));
        }

        /// <summary>
        /// Create the Texture2D that will be used as an array in the shader.
        /// </summary>
        private void CreateLayerDataArray()
        {
            if (layerArrayTexture)
                DestroyImmediate(layerArrayTexture, true);

            layerArrayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Layer Array Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            // Apply the changes to the layer data texture
            layerArrayTexture.SetPixel(tilemapLayerList.Count, 1, new Color32(0, 255, 0, 0));
            layerArrayTexture.Apply();

            // Save the layer data texture as .asset file
            AssetDatabase.AddObjectToAsset(layerArrayTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(layerArrayTexture));
        }

        /// <summary>
        /// Update the Texture2D used as an array in the shader.
        /// </summary>
        private void UpdateLayerDataArray()
        {
            int width = tilemapLayerList.Count;
            Color32[] layerData = new Color32[width];
            TilePalette[] paletteList = new TilePalette[width];

            for (int i = 0; i < width; i++)
            {
                paletteList[i] = tilemapLayerList[i].tilePalette;
                if (paletteList[i])
                    layerData[i].r = (byte)paletteList[i].tilesCount;
                layerData[i].g = (byte)(tilemapLayerList[i].intensity * 255);
            }

            DestroyImmediate(layerArrayTexture, true);

            layerArrayTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "Layer Array Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            // Apply the changes to the layer data texture
            layerArrayTexture.SetPixels32(layerData);
            layerArrayTexture.Apply();

            // Save the layer data texture as .asset file
            AssetDatabase.AddObjectToAsset(layerArrayTexture, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(layerArrayTexture));
        }

        /// <summary>
        /// Create the material used to render the Tilemap System on the mesh.
        /// </summary>
        private void CreateRenderMaterial(Shader shader)
        {
            if (renderMaterial) return;

            renderMaterial = new Material(shader)
            {
                name = "Render Material"
            };

            // Save the material as .asset file
            AssetDatabase.AddObjectToAsset(renderMaterial, tilemapData);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(renderMaterial));
        }

        /// <summary>
        /// Update the render material shader when there is a change in the render pipeline property.
        /// </summary>
        public void UpdateMaterialSettings()
        {
            if (!renderMaterial) return;

            switch (renderPipeline)
            {
                case TilemapSystemPipeline.Legacy:
                    renderMaterial.shader = legacyShader;
                    break;

                case TilemapSystemPipeline.Universal:
                    renderMaterial.shader = universalShader;
                    break;
            }
        }

        /// <summary>
        /// Copy the tilemap slice of the selected layer and save it temporarily into the clipboard property of the TilemapData
        /// </summary>
        public void CopyTilemapLayer(int layer)
        {
            if (!tilemapData) return;
            if (!tilemapTexture) return;
            if (tilemapTexture.depth < layer) return;
            tilemapData.layerClipboard = new LayerClipboard(layer, GetTexture3DSlice(tilemapTexture, layer));
        }

        /// <summary>
        /// Paste the clipboard texture from the TilemapData to the tilemap slice of the selected layer.
        /// </summary>
        public void PasteTilemapLayer(int layer)
        {
            if (!tilemapData) return;
            if (!tilemapTexture) return;
            if (tilemapTexture.depth < layer) return;
            if (tilemapData.layerClipboard.layerIndex > tilemapTexture.depth) return;
            if (!tilemapData.layerClipboard.tilemapSlice) return;
            SetTexture3DSlice(tilemapData.layerClipboard.tilemapSlice, tilemapTexture, layer);
        }

        /// <summary>
        /// Update the alpha intensity and tiles count stored in the layer data array texture.
        /// </summary>
        public void UpdateLayerSettings()
        {
            if (layerArrayTexture)
            {
                int width = tilemapLayerList.Count;
                Color32[] layerData = new Color32[width];

                if (layerData.Length == layerArrayTexture.width)
                {
                    for (int i = 0; i < width; i++)
                    {
                        if (tilemapLayerList[i].tilePalette)
                            layerData[i].r = (byte)tilemapLayerList[i].tilePalette.tilesCount;
                        layerData[i].g = (byte)(tilemapLayerList[i].intensity * 255);
                    }

                    // Apply the changes to the layer data texture
                    layerArrayTexture.SetPixels32(layerData);
                    layerArrayTexture.Apply();
                }
            }
        }

        /// <summary>
        /// Reset a layer using the selected tile.
        /// </summary>
        public void ResetLayer(int index)
        {
            Color32 color = new Color32((byte)tileIndex, 0, 0, 0);
            for (int i = 0; i < tilemapTexture.width; i++)
            {
                for (int j = 0; j < tilemapTexture.height; j++)
                {
                    tilemapTexture.SetPixel(i, j, index, color);
                }
            }

            tilemapLayerList[layerIndex].autoTileMapArray = new bool[(int)(gridMapSize.x * gridMapSize.y)];
            tilemapTexture.Apply();
        }

        /// <summary>
        /// Resize a Texture2D using Graphics.Blit().
        /// </summary>
        private void ResizeTexture2D(int width, int height, Texture2D tex2D)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);
            rt.filterMode = FilterMode.Point;
            Graphics.Blit(tex2D, rt);
            tex2D.Resize(width, height);

            //RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex2D.Apply();
            //RenderTexture.active = active;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
        }
        #endif
    }
}