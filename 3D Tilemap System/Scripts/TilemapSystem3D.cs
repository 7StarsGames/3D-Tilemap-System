using System.Collections.Generic;

/// <summary>
/// This script file contains only the runtime stuffs of the 'TilemapSystem3D' class.
/// </summary>
namespace UnityEngine.TilemapSystem3D
{
    [AddComponentMenu("Tilemap/3D Tilemap System")]
    [ExecuteInEditMode]
    public partial class TilemapSystem3D : MonoBehaviour
    {
        /// <summary>
        /// The shader used by the Standard Render Pipeline.
        /// </summary>
        public Shader legacyShader;
        
        /// <summary>
        /// The shader used by the Universal Render Pipeline.
        /// </summary>
        public Shader universalShader;
        
        /// <summary>
        /// The render pipeline used in this project.
        /// </summary>
        public TilemapSystemPipeline renderPipeline = TilemapSystemPipeline.Legacy;
        
        /// <summary>
        /// The resolution of the 3D Tileset texture.
        /// </summary>
        public int tilesetSize = 32;
        
        /// <summary>
        /// The size of the 3D Tilemap texture. It should be proportional to the terrain size or mesh uv.
        /// </summary>
        public Vector4 gridMapSize = new Vector4(32, 32, 1, 1);
        
        /// <summary>
        /// Stores the data of the current tilemap.
        /// </summary>
        public TilemapData tilemapData;
        
        /// <summary>
        /// The list of all layers in this tilemap.
        /// </summary>
        public List<TilemapSystemLayer> tilemapLayerList;
        
        /// <summary>
        /// Texture containing the tilemap data, it is used by the render material to determine which tile should be
        /// rendered in each grid position.
        /// </summary>
        public Texture3D tilemapTexture;
        
        /// <summary>
        /// Pack texture containing all the tiles used by the painting system.
        /// </summary>
        public Texture3D tilesetTexture;
        
        /// <summary>
        /// Texture used to store the tiles count and alpha intensity of each layer.
        /// In this situation is better to use a texture than a regular array because we can resize the texture every
        /// time we want, what is not possible with a shader array.
        /// Each pixel of this texture will store the data of the corespondent layer from the layer list.
        /// Channel R: Stores the tiles count on that layer.
        /// Channel G: Stores the alpha intensity of that layer.
        /// </summary>
        public Texture2D layerArrayTexture;
        
        /// <summary>
        /// The material used to render the tilemap system on the mesh.
        /// </summary>
        public Material renderMaterial;
        
        /// <summary>
        /// A block of material values to apply to the renderer component.
        /// </summary>
        public MaterialPropertyBlock materialPropertyBlock;
        
        /// <summary>
        /// Cached material property index.
        /// </summary>
        private static readonly int TilemapTexture = Shader.PropertyToID("_TilemapTexture");
        private static readonly int TilesetTexture = Shader.PropertyToID("_TilesetTexture");
        private static readonly int LayerArrayTexture = Shader.PropertyToID("_LayerArrayTexture");
        private static readonly int LayersCount = Shader.PropertyToID("_LayersCount");
        private static readonly int TilesCount = Shader.PropertyToID("_TilesCount");
        private static readonly int GridMapSize = Shader.PropertyToID("_GridMapSize");
        
        /// <summary>
        /// Translate the result from the auto tile bitmasking computation to correct layout index.
        /// </summary>
        private readonly Dictionary<int, int> m_8BitMasking = new Dictionary<int, int>()
        {
            {0, 0}, {1, 1}, {4, 2}, {5, 3}, {7, 4}, {16, 5}, {17, 6}, {20, 7}, {21, 8}, {23, 9}, {28, 10}, {29, 11}, {31, 12}, {64, 13}, {65, 14}, {68, 15},
            {69, 16}, {71, 17}, {80, 18}, {81, 19}, {84, 20}, {85, 21}, {87, 22}, {92, 23}, {93, 24}, {95, 25}, {112, 26}, {113, 27}, {116, 28}, {117, 29}, {119, 30}, {124, 31},
            {125, 32}, {127, 33}, {193, 34}, {197, 35}, {199, 36}, {209, 37}, {213, 38}, {215, 39}, {221, 40}, {223, 41}, {241, 42}, {245, 43}, {247, 44}, {253, 45}, {255, 46}
        };

        /// <summary>
        /// Called at start.
        /// </summary>
        private void Start()
        {
            if(tilemapData)
            {
                // Get the texture references stored in the TilemapData
                tilemapTexture = tilemapData.tilemapTexture;
                tilesetTexture = tilemapData.tilesetTexture;
                layerArrayTexture = tilemapData.layerArrayTexture;

                // Refresh the render material
                UpdateRenderMaterial();

                // Editor only
                #if UNITY_EDITOR
                UpdateLayerSettings();
                #endif
            }
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        private void Update()
        {
            // For an unknown reason, the render material is reseted every time the scene is saved.
            // So it is necessary to update the render material every frame only in the editor.
            // This is not necessary in the build and is not recommended for performance reasons.
            #if UNITY_EDITOR
            if (tilemapData)
                UpdateRenderMaterial();
            #endif
        }

        /// <summary>
        /// Update the render material.
        /// </summary>
        public void UpdateRenderMaterial()
        {
            // Update the material property block
            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();
            
            materialPropertyBlock.SetTexture(TilemapTexture, tilemapTexture);
            materialPropertyBlock.SetTexture(TilesetTexture, tilesetTexture);
            materialPropertyBlock.SetTexture(LayerArrayTexture, layerArrayTexture);
            materialPropertyBlock.SetInt(LayersCount, tilemapTexture.depth);
            materialPropertyBlock.SetInt(TilesCount, tilesetTexture.depth);
            materialPropertyBlock.SetVector(GridMapSize, gridMapSize);
            
            // Update the render material properties
            renderMaterial.SetTexture(TilemapTexture, tilemapTexture);
            renderMaterial.SetTexture(TilesetTexture, tilesetTexture);
            renderMaterial.SetTexture(LayerArrayTexture, layerArrayTexture);
            renderMaterial.SetInt(LayersCount, tilemapTexture.depth);
            materialPropertyBlock.SetInt(TilesCount, tilesetTexture.depth);
            materialPropertyBlock.SetVector(GridMapSize, gridMapSize);
            
            // Set the material to the terrain or custom mesh
            //#if UNITY_EDITOR
            Terrain terrainComponent = gameObject.GetComponent<Terrain>();
            if (terrainComponent)
            {
                terrainComponent.materialTemplate = renderMaterial;
                terrainComponent.SetSplatMaterialPropertyBlock(materialPropertyBlock);
            }
            
            Renderer rendererComponent = gameObject.GetComponent<Renderer>();
            if (rendererComponent)
            {
                rendererComponent.material = renderMaterial;
                rendererComponent.SetPropertyBlock(materialPropertyBlock);
            }
            //#endif
        }

        /// <summary>
        /// Sets the alpha intensity of a given layer.
        /// </summary>
        public void SetLayerIntensity(int layer, float intensity)
        {
            if (layerArrayTexture)
            {
                intensity = Mathf.Clamp01(intensity);
                tilemapLayerList[layer].intensity = intensity;
                Color32 color = layerArrayTexture.GetPixel(layer, 0);
                color.g = (byte)(intensity * 255);
                layerArrayTexture.SetPixel(layer, 0, color);
                layerArrayTexture.Apply();
            }
        }

        /// <summary>
        /// Gets the alpha intensity of a given layer.
        /// </summary>
        public float GetLayerIntensity(int layer)
        {
            return tilemapLayerList[layer].intensity;
        }
        
        /// <summary>
        /// Pickup the index of the clicked tile on the mesh.
        /// </summary>
        public int PickupTile(int x, int y, int layer)
        {
            return (int) (tilemapTexture.GetPixel(x, y, layer).r * 255);
        }
        
        /// <summary>
        /// Paint a tile on the mesh.
        /// </summary>
        public void PaintTile(int x, int y, int index, int layer)
        {
            index = Mathf.Clamp(index, 0, tilemapLayerList[layer].tilePalette.tilesCount - 1);
            
            if (index > 0)
                tilemapLayerList[layer].autoTileMapArray[(y * tilemapTexture.width) + x] = true;
            else
                tilemapLayerList[layer].autoTileMapArray[(y * tilemapTexture.width) + x] = false;
            
            tilemapTexture.SetPixel(x, y, layer, new Color32((byte) index, 0, 0, 0));
            tilemapTexture.Apply();
        }

        // Based on "TextureFloodFill" from:
        // https://wiki.unity3d.com/index.php/TextureFloodFill
        /// <summary>
        /// Fill a layer region with a tile.
        /// </summary>
        public void FillTile(int x, int y, int index, int layer)
        {
            index = Mathf.Clamp(index, 0, tilemapLayerList[layer].tilePalette.tilesCount - 1);
            Texture2D tempTexture2D = GetTexture3DSlice(tilemapTexture, layer);
            var width = tilemapTexture.width;
            var height = tilemapTexture.height;
            var colors = tempTexture2D.GetPixels32();
            var refCol = colors[x + y * width];
            var nodes = new Queue<Vector2Int>();
            nodes.Enqueue(new Vector2Int(x, y));
            
            while (nodes.Count > 0)
            {
                var currentPoint = nodes.Dequeue();
                
                for (var i = currentPoint.x; i < width; i++)
                {
                    int C = colors[i + currentPoint.y * width].r;
                    if (C != refCol.r || C == index)
                        break;
                    colors[i + currentPoint.y * width] = new Color32((byte)index, 0, 0, 0);
                    if (currentPoint.y + 1 < height)
                    {
                        C = colors[i + currentPoint.y * width + width].r;
                        if (C == refCol.r && C != index)
                            nodes.Enqueue(new Vector2Int(i, currentPoint.y + 1));
                    }
                    
                    if (currentPoint.y - 1 < 0) continue;
                    
                    C = colors[i + currentPoint.y * width - width].r;
                    if (C == refCol.r && C != index)
                        nodes.Enqueue(new Vector2Int(i, currentPoint.y - 1));
                }
                
                for (var i = currentPoint.x - 1; i >= 0; i--)
                {
                    int C = colors[i + currentPoint.y * width].r;
                    if (C != refCol.r || C == index)
                        break;
                    colors[i + currentPoint.y * width] = new Color32((byte)index, 0, 0, 0);
                    if (currentPoint.y + 1 < height)
                    {
                        C = colors[i + currentPoint.y * width + width].r;
                        if (C == refCol.r && C != index)
                            nodes.Enqueue(new Vector2Int(i, currentPoint.y + 1));
                    }
                    
                    if (currentPoint.y - 1 < 0) continue;
                    
                    C = colors[i + currentPoint.y * width - width].r;
                    if (C == refCol.r && C != index)
                        nodes.Enqueue(new Vector2Int(i, currentPoint.y - 1));
                }
            }
            
            tempTexture2D.SetPixels32(colors);
            tempTexture2D.Apply();
            SetTexture3DSlice(tempTexture2D, tilemapTexture, layer);
        }
        
        /// <summary>
        /// Perform the auto tile painting.
        /// </summary>
        public void PaintAutoTile(int x, int y, int layoutIndex, int layer)
        {
            int autoTileMapX = x;
            int autoTileMapY = y;
            int north;
            int northEast;
            int east;
            int southEast;
            int south;
            int southWest;
            int west;
            int northWest;
            int autoTileIndex;
            
            tilemapLayerList[layer].autoTileMapArray[y * tilemapTexture.width + x] = true;
            
            // Compute the center tile
            north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
            east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
            south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
            west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
            // if top = 0: topLeft = 0, topRight = 0
            // if left = 0: topLeft = 0, bottomLeft = 0
            northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
            southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
            southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
            northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
            autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
            tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            
            // Compute the north tile
            autoTileMapX = x;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the north-east tile
            autoTileMapX = x + 1;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the east tile
            autoTileMapX = x + 1;
            autoTileMapY = y;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the east-south tile
            autoTileMapX = x + 1;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the south tile
            autoTileMapX = x;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the south-west tile
            autoTileMapX = x - 1;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the west tile
            autoTileMapX = x - 1;
            autoTileMapY = y ;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the west-north tile
            autoTileMapX = x - 1;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            tilemapTexture.Apply();
        }
        
        /// <summary>
        /// Erase the tile from the auto tile array map.
        /// </summary>
        public void EraseAutoTile(int x, int y, int layoutIndex, int layer)
        {
            int autoTileMapX = x;
            int autoTileMapY = y;
            int north;
            int northEast;
            int east;
            int southEast;
            int south;
            int southWest;
            int west;
            int northWest;
            int autoTileIndex;
            
            // Erase the clicked tile
            tilemapLayerList[layer].autoTileMapArray[y * tilemapTexture.width + x] = false;
            tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) 0, 0, 0, 0));
            
            // Compute the north tile
            autoTileMapX = x;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the north-east tile
            autoTileMapX = x + 1;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the east tile
            autoTileMapX = x + 1;
            autoTileMapY = y;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the east-south tile
            autoTileMapX = x + 1;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the south tile
            autoTileMapX = x;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the south-west tile
            autoTileMapX = x - 1;
            autoTileMapY = y - 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the west tile
            autoTileMapX = x - 1;
            autoTileMapY = y ;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            // Compute the west-north tile
            autoTileMapX = x - 1;
            autoTileMapY = y + 1;
            if (GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY))
            {
                north = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY + 1) ? 1 : 0;
                east  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray,autoTileMapX + 1, autoTileMapY) ? 4 : 0;
                south = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX, autoTileMapY - 1) ? 16 : 0;
                west  = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY) ? 64 : 0;
                northEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY + 1) && north != 0 && east != 0 ? 2 : 0;
                southEast = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX + 1, autoTileMapY - 1) && south != 0 && east != 0 ? 8 : 0;
                southWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY - 1) && south != 0 && west != 0 ? 32 : 0;
                northWest = GetArrayMapIndex(tilemapLayerList[layer].autoTileMapArray, autoTileMapX - 1, autoTileMapY + 1) && north != 0 && west != 0 ? 128 : 0;
                autoTileIndex = north + northEast + east + southEast + south + southWest + west + northWest;
                tilemapTexture.SetPixel(autoTileMapX, autoTileMapY, layer, new Color32((byte) tilemapLayerList[layer].autoTileList[layoutIndex].id[m_8BitMasking[autoTileIndex]], 0, 0, 0));
            }
            
            tilemapTexture.Apply();
        }

        /// <summary>
        /// Copy a tilemap slice and save into a Texture2D.
        /// </summary>
        private Texture2D GetTexture3DSlice(Texture3D texture, int depth)
        {
            // Creates the temporary Texture2D
            Texture2D tempTexture = new Texture2D(texture.width, texture.height, texture.format, false)
            {
                name = "Slice Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            // Copy the 3D texture slice color to the temporary Texture2D
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    Color32 tempColor = texture.GetPixel(i, j, depth);
                    tempTexture.SetPixel(i, j, tempColor);
                }
            }

            // Apply the changes to the temporary Texture2D and return
            tempTexture.Apply();
            return tempTexture;
        }

        /// <summary>
        /// Paste a Texture2D to a tilemap slice.
        /// </summary>
        private void SetTexture3DSlice(Texture2D sourceTex2D, Texture3D targetTex3D, int depth)
        {
            // Get the color of the source Texture2D
            Color32[] newColor = sourceTex2D.GetPixels32();

            // Used to access each index of the color array
            int index = 0;

            // Paste the Texture2D color into the tilemap slice pixel by pixel
            for (int i = 0; i < targetTex3D.width; i++)
            {
                for (int j = 0; j < targetTex3D.height; j++)
                {
                    targetTex3D.SetPixel(j, i, depth, newColor[index]);
                    index++;
                }
            }

            // Apply the changes to the tilemap texture
            targetTex3D.Apply();
        }

        /// <summary>
        /// Get the value of an array using a 2D coordinate as a parameter.
        /// </summary>
        private bool GetArrayMapIndex(bool[] array, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < tilemapTexture.width && y < tilemapTexture.height)
                return array[y * tilemapTexture.width + x];
            else
                return false;
        }
    }
}