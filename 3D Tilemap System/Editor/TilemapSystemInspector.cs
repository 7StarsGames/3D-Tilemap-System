using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.TilemapSystem3D;

[CustomEditor(typeof(TilemapSystem3D))]
public class TilemapSystemInspector : Editor
{
    // The target script
    private TilemapSystem3D m_target;

    // Editor only
    public Texture2D emptyIconTexture;
    public TilePalette blobAutoTileLayout;
    private Texture2D m_layerIconTexture;
    private readonly Color m_greenColor = new Color(0.85f, 1.0f, 0.85f);
    private readonly Color m_redColor = new Color(1.0f, 0.75f, 0.75f);
    private readonly Color m_selectedTileColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
    private Texture[] m_paintingToolIcons;
    private bool m_isPicking, m_isPainting, m_isFilling, m_isErasing;
    private bool m_isPaintingEvent;
    private readonly int[] m_tilesetSizeArray = new[] {16, 24, 32, 48, 64, 96, 128};
    private string[] m_autoTileElements;
    private readonly GUIContent[] m_tilesetSizeContentArray = new GUIContent[]
    {
        new GUIContent("16x16", "The resolution of the 3D Tileset texture."),
        new GUIContent("24x24", "The resolution of the 3D Tileset texture."),
        new GUIContent("32x32", "The resolution of the 3D Tileset texture."),
        new GUIContent("48x48", "The resolution of the 3D Tileset texture."),
        new GUIContent("64x64", "The resolution of the 3D Tileset texture."),
        new GUIContent("96x96", "The resolution of the 3D Tileset texture."),
        new GUIContent("128x128", "The resolution of the 3D Tileset texture.")
        //new GUIContent("192x192", "The resolution of each tile."),
        //new GUIContent("256x256", "The resolution of each tile.")
    };

    // Tile grid
    private int m_buttonIndex;
    private bool m_isSelected;
    private Vector2 m_scrollPos, m_autoTileScrollPos;
    private Rect m_controlRect;
    private Texture2D m_tileTexture;

    // GUIContents
    private readonly GUIContent[] m_guiContent = new[]
    {
        new GUIContent("Grid Map Size:", "The size of the 3D Tilemap texture. It should be proportional to the terrain size or mesh uv."),
        new GUIContent("Width", "The width of tilemap texture."),
        new GUIContent("Height", "The height of tilemap texture."),
        new GUIContent("Tilemap Layers", "List of layers that will be used in this tilemap."),
        new GUIContent("Paint Layer", "Select the layer for painting."),
        new GUIContent("Tile Palette", "The profile containing the tiles palette."),
        new GUIContent("Tilemap Data", "Reference to the tilemap data, it is usually created automatically by pressing the button below. It stores the tilemap texture and the render material."),
        new GUIContent("Legacy Shader", "Diffuse shader used by the Legacy Render Pipeline."),
        new GUIContent("Universal Shader", "Diffuse shader used by the Universal Render Pipeline."),
        new GUIContent("Render Pipeline", "Select the render pipeline you are using in this project."),
        new GUIContent("Painting Mode", "Select the feature used to paint the tiles."),
        new GUIContent("Auto Tile List", "List of auto tile layouts that will be used by this layer."),
        new GUIContent("Auto", "Automatically setup the layout using the current tile selected in the palette as starting point."),
        new GUIContent("Tilemap Texture", "The 3D texture that stores the tilemap data of each layer used by this component. This data is used by the render material as mask to render on the mesh the proper tile slice of the 3D Tileset texture."),
        new GUIContent("Render Material", "The material used to render the tiles on the mesh."),
        new GUIContent("Tileset Texture", "The 3D texture that stores all the tiles used by this component."),
        new GUIContent("Tileset Size", "The resolution of the 3D Tileset texture."),
        new GUIContent("Layout", "Select the layout index that you want to paint."),
        new GUIContent("Donate", "Make a donation to support the asset developer."),
        new GUIContent("Azure[Sky] Dynamic Skybox", "Complete sky system with day-night cycle and weather solution."),
        new GUIContent("Azure[Sky] Lite", "A lite version of Azure[Sky] Dynamic Skybox with less features and best performance.")
    };

    // Temporary
    private Vector4 m_gridMapSize = new Vector4(32, 32, 1, 1);

    // Serialized properties
    private SerializedProperty m_showReferencesGroup;
    private SerializedProperty m_showSettingsGroup;
    private SerializedProperty m_showDataGroup;
    private SerializedProperty m_showPantingGroup;
    private SerializedProperty m_showAutoTileGroup;
    private SerializedProperty m_showRandomTilePaintingGroup;
    private SerializedProperty m_showRandomizeTileGroup;
    private SerializedProperty m_showTileBrushesGroup;
    private SerializedProperty m_showAboutGroup;
    private SerializedProperty m_legacyShader;
    private SerializedProperty m_universalShader;
    private SerializedProperty m_tilemapData;
    private SerializedProperty m_tilePalette;
    private SerializedProperty m_renderPipeline;
    private SerializedProperty m_tilesetSize;
    private SerializedProperty m_paintingMode;
    private SerializedProperty m_autoTileLayoutIndex;

    // Reorderable lists
    private SerializedProperty m_tilemapLayerList;
    private ReorderableList m_reorderableTilemapLayerList;
    private readonly Dictionary<int, ReorderableList> m_autoTileDictionary = new Dictionary<int, ReorderableList>();

    private void OnDisable()
    {
        // Shows the editor tools again
        Tools.hidden = false;
        m_target.toolIndex = 0;
    }
    
    private void OnEnable()
    {
        // Get Target
        m_target = (TilemapSystem3D) target;

        // Refresh the render material
        if (m_target.tilemapData)
            m_target.UpdateRenderMaterial();
        
        // Update layer intensity
        m_target.UpdateLayerSettings();
        
        // Find the serialized properties
        m_showReferencesGroup = serializedObject.FindProperty("showReferencesGroup");
        m_showSettingsGroup = serializedObject.FindProperty("showSettingsGroup");
        m_showDataGroup = serializedObject.FindProperty("showDataGroup");
        m_showPantingGroup = serializedObject.FindProperty("showPantingGroup");
        m_showAutoTileGroup = serializedObject.FindProperty("showAutoTileGroup");
        m_showRandomTilePaintingGroup = serializedObject.FindProperty("showRandomTilePaintingGroup");
        m_showRandomizeTileGroup = serializedObject.FindProperty("showRandomizeTileGroup");
        m_showTileBrushesGroup = serializedObject.FindProperty("showTileBrushesGroup");
        m_showAboutGroup = serializedObject.FindProperty("showAboutGroup");
        m_legacyShader = serializedObject.FindProperty("legacyShader");
        m_universalShader = serializedObject.FindProperty("universalShader");
        m_tilemapData = serializedObject.FindProperty("tilemapData");
        m_renderPipeline = serializedObject.FindProperty("renderPipeline");
        m_tilesetSize = serializedObject.FindProperty("tilesetSize");
        m_paintingMode = serializedObject.FindProperty("paintingMode");
        
        m_paintingToolIcons = new[]
        {
            EditorGUIUtility.FindTexture("Grid.PickingTool"),
            EditorGUIUtility.FindTexture("Grid.PaintTool"),
            EditorGUIUtility.FindTexture("Grid.FillTool"),
            EditorGUIUtility.FindTexture("Grid.EraserTool")
        };
        
        // Layer list
        m_tilemapLayerList = serializedObject.FindProperty("tilemapLayerList");
        m_reorderableTilemapLayerList = new ReorderableList(serializedObject, m_tilemapLayerList, false, true, true, true)
        {
            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Initialize
                var element = m_reorderableTilemapLayerList.serializedProperty.GetArrayElementAtIndex(index);
                
                // Draw the image icon
                if(m_target.tilemapLayerList[index].tilePalette)
                    m_layerIconTexture = m_target.tilemapLayerList[index].tilePalette.temporaryTileTextureArray[1];
                if (m_layerIconTexture == null) m_layerIconTexture = emptyIconTexture;
                GUI.DrawTexture(new Rect(rect.x, rect.y + 2, 38, 38), m_layerIconTexture, ScaleMode.StretchToFill, true, 0);
                m_layerIconTexture = null;
                
                // Draw the name field
                EditorGUI.PropertyField(new Rect(rect.x + 42, rect.y + 1, rect.width - 85, 18), element.FindPropertyRelative("layerName"), GUIContent.none);
                
                // Draw the alpha intensity slider
                EditorGUI.Slider(new Rect(rect.x + 42, rect.y + 22, rect.width - 85, 18), element.FindPropertyRelative("intensity"), 0.0f, 1.0f, GUIContent.none);
                
                // Copy button
                if (GUI.Button(new Rect(rect.width - 18, rect.y + 1, 22, 41), "C"))
                {
                    m_target.CopyTilemapLayer(index);
                }
                
                // Paste button
                if (GUI.Button(new Rect(rect.width + 4, rect.y + 1, 22, 41), "P"))
                {
                    m_target.PasteTilemapLayer(index);
                }
                
                // Create the reorderable auto tile list for this layer and save it to the dictionary
                if (!m_autoTileDictionary.ContainsKey(index))
                {
                    m_autoTileDictionary[index] = CreateAutoTileList(element.FindPropertyRelative("autoTileList"));
                }
            },
            
            onAddCallback = (ReorderableList l) =>
            {
                var index = l.serializedProperty.arraySize;
                ReorderableList.defaultBehaviours.DoAddButton(l);
                var element = l.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("layerName").stringValue = "Layer " + index;
                element.FindPropertyRelative("intensity").floatValue = 1.0f;
                element.FindPropertyRelative("tilePalette").objectReferenceValue = null;
                element.FindPropertyRelative("autoTileList").ClearArray();
                ResetAutoTileArrayMap(element.FindPropertyRelative("autoTileMapArray"));
            },
            
            onRemoveCallback = (ReorderableList l) =>
            {
                m_target.layerIndex--;
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
            },
            
            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, m_guiContent[3], EditorStyles.boldLabel);
            },
            
            elementHeightCallback = (int index) => 44,
            
            drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    GUI.Box(new Rect(rect.x +2, rect.y -1, rect.width -4, rect.height +1), "","selectionRect");
            }
        };
        
        if (m_reorderableTilemapLayerList.count > 0)
            m_reorderableTilemapLayerList.index = 0;
    }
    
    /// <summary>
    /// Custom Inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Start custom Inspector
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        m_target.layerIndex = m_reorderableTilemapLayerList.index;
        
        if (m_target.toolIndex > 0 && m_target.toolIndex < 5)
            Tools.hidden = true;
        else
            Tools.hidden = false;
        //
        // References
        //
        m_showReferencesGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showReferencesGroup.isExpanded , "References");
        if (m_showReferencesGroup.isExpanded)
        {
            GUILayout.Space(1);
            // Standard diffuse shader
            GUI.color = m_greenColor;
            if (!m_legacyShader.objectReferenceValue) GUI.color = m_redColor;
            EditorGUILayout.PropertyField(m_legacyShader, m_guiContent[7]);
            
            // Universal diffuse shader
            GUI.color = m_greenColor;
            if (!m_universalShader.objectReferenceValue) GUI.color = m_redColor;
            EditorGUILayout.PropertyField(m_universalShader, m_guiContent[8]);
            GUI.color = Color.white;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //
        // Settings
        //
        m_showSettingsGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showSettingsGroup.isExpanded , "Settings");
        if (m_showSettingsGroup.isExpanded)
        {
            GUILayout.Space(1);
            // Render pipeline
            EditorGUILayout.PropertyField(m_renderPipeline, m_guiContent[9]);
            if(m_target.renderPipeline == TilemapSystemPipeline.Universal)
                EditorGUILayout.HelpBox("Universal Render Pipeline is still a work in progress and is not fully functional.", MessageType.Info);
            
            // Tilemap size
            EditorGUILayout.IntPopup(m_tilesetSize, m_tilesetSizeContentArray, m_tilesetSizeArray, m_guiContent[16]);
            
            // Grid size
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(m_guiContent[0]);
            m_gridMapSize.x = EditorGUILayout.IntField(m_guiContent[1], (int)m_target.gridMapSize.x);
            m_gridMapSize.y = EditorGUILayout.IntField(m_guiContent[2], (int)m_target.gridMapSize.y);
            EditorGUILayout.EndVertical();
            
            // Layer list
            m_reorderableTilemapLayerList.DoLayoutList();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //
        // Data
        //
        m_showDataGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showDataGroup.isExpanded , "Data");
        if (m_showDataGroup.isExpanded)
        {
            GUILayout.Space(1);
            if (m_reorderableTilemapLayerList.count > 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                // Tilemap data
                if (!m_tilemapData.objectReferenceValue) GUI.color = m_redColor;
                EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), m_tilemapData, m_guiContent[6]);
                GUI.color = Color.white;
                // Tilemap texture field
                if (!m_target.tilemapTexture) GUI.color = m_redColor;
                EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), m_guiContent[13], m_target.tilemapTexture, typeof(Texture3D), false);
                GUI.color = Color.white;
                // Tileset texture field
                if (!m_target.tilesetTexture) GUI.color = m_redColor;
                EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), m_guiContent[15], m_target.tilesetTexture, typeof(Texture3D), false);
                GUI.color = Color.white;
                // Array texture field
                if (!m_target.layerArrayTexture) GUI.color = m_redColor;
                EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), m_guiContent[17], m_target.layerArrayTexture, typeof(Texture2D), false);
                GUI.color = Color.white;
                // Render material field
                if (!m_target.renderMaterial) GUI.color = m_redColor;
                EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), m_guiContent[14], m_target.renderMaterial, typeof(Material), false);
                GUI.color = Color.white;
                EditorGUI.EndDisabledGroup();
                
                // Button
                if (GUILayout.Button("Generate Tilemap Data"))
                {
                    m_target.GenerateTilemapData();
                    if (m_target.tilemapData)
                    {
                        TilePalette[] palettes = new TilePalette[m_target.tilemapLayerList.Count];
                        int[] tilesCount = new int[m_target.tilemapLayerList.Count];
                        for (int i = 0; i < palettes.Length; i++)
                        {
                            palettes[i] = m_target.tilemapLayerList[i].tilePalette;
                            if (palettes[i])
                                tilesCount[i] = m_target.tilemapLayerList[i].tilePalette.tilesCount;
                            else
                                tilesCount[i] = 0;
                        }
                        
                        m_target.tilemapData.settingsState = new TilemapSettings(m_target.tilemapLayerList.Count, tilesCount, palettes, (int) m_gridMapSize.x, (int) m_gridMapSize.y, m_tilesetSize.intValue);
                        m_target.tilemapData.layerIntensity = new float[m_target.tilemapLayerList.Count];
                    }
                }
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //
        // Painting
        //
        m_showPantingGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showPantingGroup.isExpanded , "Painting");
        if (m_showPantingGroup.isExpanded)
        {
            if (m_reorderableTilemapLayerList.count > 0 && m_target.layerIndex >= 0 && m_target.tilemapLayerList.Count > 0)
            {
                if (m_tilemapData.objectReferenceValue)
                {
                    // Tile palette field
                    EditorGUILayout.BeginVertical("Box");
                    m_tilePalette = m_reorderableTilemapLayerList.serializedProperty.GetArrayElementAtIndex(m_target.layerIndex).FindPropertyRelative("tilePalette");
                    GUI.color = m_greenColor;
                    if (!m_tilePalette.objectReferenceValue) GUI.color = m_redColor;
                    EditorGUILayout.PropertyField(m_tilePalette, m_guiContent[5]);
                    GUI.color = Color.white;
                    EditorGUILayout.EndVertical();
                    
                    if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette)
                    {
                        // Painting mode
                        EditorGUILayout.BeginVertical("Box");
                        EditorGUILayout.PropertyField(m_paintingMode, m_guiContent[10]);
                        
                        // Auto tile layout popup
                        if (m_target.paintingMode == TilemapSystemPaintingMode.AutoTiles)
                        {
                            m_autoTileElements = GetAutoTileElements();
                            
                            if (m_autoTileElements.Length > 0)
                            {
                                m_autoTileLayoutIndex = m_reorderableTilemapLayerList.serializedProperty.GetArrayElementAtIndex(m_target.layerIndex).FindPropertyRelative("layoutIndex");
                                m_autoTileLayoutIndex.intValue = EditorGUILayout.Popup(m_guiContent[17], m_autoTileLayoutIndex.intValue, m_autoTileElements);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("The Auto Tile list is empty. Please, set some auto tile layout first.", MessageType.Error);
                            }
                        }
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(-3);
                        
                        if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount > 0)
                        {
                            EditorGUILayout.BeginVertical("Box");
                            if (m_target.tilesetTexture)
                            {
                                if (CanPaint())
                                {
                                    // Reset layer button
                                    if (GUILayout.Button("Reset This Layer Using the Selected Tile"))
                                    {
                                        // Register the texture in the undo stack
                                        Undo.RegisterCompleteObjectUndo(m_target.tilemapTexture, "Tilemap Change");
                                        m_target.ResetLayer(m_target.layerIndex);
                                    }
                                    
                                    // Toolbar
                                    EditorGUILayout.BeginHorizontal();
                                    m_isPicking = m_target.toolIndex == 1 ? true : false;
                                    if (GUILayout.Toggle(m_isPicking, m_paintingToolIcons[0], EditorStyles.miniButtonLeft))
                                        m_target.toolIndex = 1;
                                    else if (m_target.toolIndex == 1)
                                        m_target.toolIndex = 0;
                                    
                                    m_isPainting = m_target.toolIndex == 2 ? true : false;
                                    if (GUILayout.Toggle(m_isPainting, m_paintingToolIcons[1], EditorStyles.miniButtonMid))
                                        m_target.toolIndex = 2;
                                    else if (m_target.toolIndex == 2)
                                        m_target.toolIndex = 0;
                                    
                                    m_isFilling = m_target.toolIndex == 3 ? true : false;
                                    if (GUILayout.Toggle(m_isFilling, m_paintingToolIcons[2], EditorStyles.miniButtonMid))
                                        m_target.toolIndex = 3;
                                    else if (m_target.toolIndex == 3)
                                        m_target.toolIndex = 0;
                                    
                                    m_isErasing = m_target.toolIndex == 4 ? true : false;
                                    if (GUILayout.Toggle(m_isErasing, m_paintingToolIcons[3], EditorStyles.miniButtonRight))
                                        m_target.toolIndex = 4;
                                    else if (m_target.toolIndex == 4)
                                        m_target.toolIndex = 0;
                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                    EditorGUILayout.HelpBox("It looks like you have changed some settings. Please, regenerate the Tilemap Data to update the 3D Textures.", MessageType.Error);
                            }
                            else
                                EditorGUILayout.HelpBox("There is no Tileset Texture yet. Please, regenerate the Tilemap Data again to create the 3D Tileset texture so you can start painting.", MessageType.Error);
                            
                            // Tile grid
                            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                            EditorGUILayout.LabelField("", GUILayout.Width(442));
                            GUILayout.Space(-20);
                            m_buttonIndex = 0;
                            
                            // Start the selectable loop
                            for (int vertical = 0; vertical < 256;)
                            {
                                m_controlRect = EditorGUILayout.GetControlRect(GUILayout.Width(24), GUILayout.Height(24));
                                
                                EditorGUILayout.BeginHorizontal();
                                for (int horizontal = 0; horizontal < 16; horizontal++)
                                {
                                    // Get the tile texture
                                    m_tileTexture = m_target.tilemapLayerList[m_target.layerIndex].tilePalette.temporaryTileTextureArray[vertical] ? m_target.tilemapLayerList[m_target.layerIndex].tilePalette.temporaryTileTextureArray[vertical] : emptyIconTexture;
                                    
                                    // Draw the selectable button
                                    m_isSelected = m_target.tileIndex == m_buttonIndex ? true : false;
                                    if (GUI.Toggle(m_controlRect, m_isSelected, GUIContent.none, GUI.skin.button))
                                    {
                                        m_target.tileIndex = m_buttonIndex;
                                    }
                                    
                                    // Draw the tile texture
                                    if (m_isSelected) GUI.color = m_selectedTileColor;
                                    GUI.DrawTexture(m_controlRect, m_tileTexture, ScaleMode.StretchToFill, true, 0);
                                    GUI.color = Color.white;
                                    
                                    m_controlRect.x += 28;
                                    vertical++;
                                    m_buttonIndex++;
                                }
                                
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUILayout.EndScrollView();
                            EditorGUILayout.EndVertical();
                        }
                        else
                            EditorGUILayout.HelpBox("The Tile Palette used in this layer is empty. Please, add the tiles to the Tile Palette first.", MessageType.Error);
                    }
                    else
                        EditorGUILayout.HelpBox("The tile palette of this layer is missing! Add a Tile Palette to start painting.", MessageType.Error);
                }
                else
                    EditorGUILayout.HelpBox("There is no tilemap data created yet! Generate the Tilemap Data.", MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
            
            
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //
        // Auto tiles
        //
        m_showAutoTileGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAutoTileGroup.isExpanded , "Auto Tiles");
        if (m_showAutoTileGroup.isExpanded)
        {
            if (m_reorderableTilemapLayerList.count > 0 && m_target.layerIndex >= 0 && m_target.tilemapLayerList.Count > 0)
            {
                if (m_tilemapData.objectReferenceValue)
                {
                    if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette)
                    {
                        if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount > 0)
                        {
                            // Editor stuff here
                            GUILayout.Space(3);
                            m_autoTileDictionary[m_target.layerIndex].DoLayoutList();
                        }
                        else
                            EditorGUILayout.HelpBox("The Tile Palette used in this layer is empty. Please, add the tiles to the Tile Palette first.", MessageType.Error);
                    }
                    else
                        EditorGUILayout.HelpBox("The tile palette of this layer is missing! Add a Tile Palette to start painting.", MessageType.Error);
                }
                else
                    EditorGUILayout.HelpBox("There is no tilemap data created yet! Generate the Tilemap Data.", MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //
        // Random tile painting
        //
        EditorGUI.BeginDisabledGroup(true);
        m_showRandomTilePaintingGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showRandomTilePaintingGroup.isExpanded , "Random Tile Painting");
        if (m_showRandomTilePaintingGroup.isExpanded)
        {
            if (m_reorderableTilemapLayerList.count > 0 && m_target.layerIndex >= 0 && m_target.tilemapLayerList.Count > 0)
            {
                if (m_tilemapData.objectReferenceValue)
                {
                    if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette)
                    {
                        if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount > 0)
                        {
                            // Editor stuff here
                        }
                        else
                            EditorGUILayout.HelpBox("The Tile Palette used in this layer is empty. Please, add the tiles to the Tile Palette first.", MessageType.Error);
                    }
                    else
                        EditorGUILayout.HelpBox("The tile palette of this layer is missing! Add a Tile Palette to start painting.", MessageType.Error);
                }
                else
                    EditorGUILayout.HelpBox("There is no tilemap data created yet! Generate the Tilemap Data.", MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // Random tiles
        m_showRandomizeTileGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showRandomizeTileGroup.isExpanded , "Randomize Tiles");
        if (m_showRandomizeTileGroup.isExpanded)
        {
            if (m_reorderableTilemapLayerList.count > 0 && m_target.layerIndex >= 0 && m_target.tilemapLayerList.Count > 0)
            {
                if (m_tilemapData.objectReferenceValue)
                {
                    if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette)
                    {
                        if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount > 0)
                        {
                            // Editor stuff here
                        }
                        else
                            EditorGUILayout.HelpBox("The Tile Palette used in this layer is empty. Please, add the tiles to the Tile Palette first.", MessageType.Error);
                    }
                    else
                        EditorGUILayout.HelpBox("The tile palette of this layer is missing! Add a Tile Palette to start painting.", MessageType.Error);
                }
                else
                    EditorGUILayout.HelpBox("There is no tilemap data created yet! Generate the Tilemap Data.", MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // Tile brushes
        m_showTileBrushesGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showTileBrushesGroup.isExpanded , "Tile Brushes");
        if (m_showTileBrushesGroup.isExpanded)
        {
            if (m_reorderableTilemapLayerList.count > 0 && m_target.layerIndex>= 0 && m_target.tilemapLayerList.Count > 0)
            {
                if (m_tilemapData.objectReferenceValue)
                {
                    if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette)
                    {
                        if (m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount > 0)
                        {
                            // Editor stuff here
                        }
                        else
                            EditorGUILayout.HelpBox("The Tile Palette used in this layer is empty. Please, add the tiles to the Tile Palette first.", MessageType.Error);
                    }
                    else
                        EditorGUILayout.HelpBox("The tile palette of this layer is missing! Add a Tile Palette to start painting.", MessageType.Error);
                }
                else
                    EditorGUILayout.HelpBox("There is no tilemap data created yet! Generate the Tilemap Data.", MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("There is no layer created yet! Create a tilemap layer.", MessageType.Error);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUI.EndDisabledGroup();

        // About group
        m_showAboutGroup.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAboutGroup.isExpanded, "About");
        if (m_showAboutGroup.isExpanded)
        {
            EditorGUILayout.HelpBox("3D Tilemap System v1.0.1 by Seven Stars Games", MessageType.None);
            if (GUILayout.Button(m_guiContent[18]))
            {
                Application.OpenURL("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S8AB7CVH5VMZS&source=url");
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("My other assets you may like:");
            if (GUILayout.Button(m_guiContent[19]))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/particles-effects/azure-sky-dynamic-skybox-36050");
            }
            if (GUILayout.Button(m_guiContent[20]))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/azure-sky-lite-89858");
            }
        }

        // Update layer intensity when there is a change in the Inspector
        if (m_target.tilemapData)
        {
            if (NeedUpdateLayerIntensity())
            {
                m_target.UpdateLayerSettings();
                m_target.tilemapData.layerIntensity = new float[m_target.tilemapLayerList.Count];
                for (int i = 0; i < m_target.tilemapLayerList.Count; i++)
                {
                    m_target.tilemapData.layerIntensity[i] = m_target.tilemapLayerList[i].intensity;
                }
            }
        }
        
        // End custom Inspector
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_target, "3D Tilemap System");
            serializedObject.ApplyModifiedProperties();
            m_target.gridMapSize = m_gridMapSize;
            m_target.UpdateMaterialSettings();
        }
    }
    
    /// <summary>
    /// Editor painting in scene view
    /// </summary>
    private void OnSceneGUI()
    {
        if ((m_target.toolIndex > 0 || m_target.toolIndex <= 4))
        {
            var e = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            if (e.button == 0 && !e.alt && e.type != EventType.MouseMove)
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        m_isPaintingEvent = true;
                        if (!m_target.tilemapData)
                        {
                            Debug.LogWarning("<b>3D Tilemap System:</b> The tilemap data is missing! Please, first generate the tilemap data to be able to paint the tiles.");
                            m_isPaintingEvent = false;
                            e.Use();
                            return;
                        }
                        break;
                    
                    case EventType.Layout:
                        HandleUtility.AddDefaultControl(controlID);
                        break;
                    
                    case EventType.MouseUp:
                        m_isPaintingEvent = false;
                        e.Use();
                        break;
                }
            }
            
            if (e.alt && m_isPaintingEvent == true)
            {
                m_isPaintingEvent = false;
            }
            
            if (!m_isPaintingEvent) return;
            
            var mousePos = e.mousePosition;
            var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - mousePos.y * pixelsPerPoint;
            mousePos.x *= pixelsPerPoint;
            
            var ray = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit)) return;
            
            // Get the uv texture coordinate at the collision location
            var hitCoord = hit.textureCoord;
            hitCoord.x *= m_target.tilemapTexture.width;
            hitCoord.y *= m_target.tilemapTexture.height;
                    
            // Register the texture in the undo stack
            Undo.RegisterCompleteObjectUndo(m_target.tilemapTexture, "Tilemap Change");
            
            // Pickup the tile
            if (m_target.toolIndex == 1)
            {
                m_target.tileIndex =  m_target.PickupTile((int) hitCoord.x, (int) hitCoord.y, m_target.layerIndex);
                m_isPaintingEvent = false;
                Repaint();
            }
            
            // Paint the tile
            if (m_target.toolIndex == 2 && m_target.paintingMode == TilemapSystemPaintingMode.Default)
                m_target.PaintTile((int)hitCoord.x, (int)hitCoord.y, m_target.tileIndex, m_target.layerIndex);
            
            // Fill the area with the same tile
            if (m_target.toolIndex == 3 && m_target.paintingMode == TilemapSystemPaintingMode.Default)
            {
                m_target.FillTile((int) hitCoord.x, (int) hitCoord.y, m_target.tileIndex, m_target.layerIndex);
                m_isPaintingEvent = false;
            }
            
            // Auto tile painting
            if (m_target.toolIndex == 2 && m_target.paintingMode == TilemapSystemPaintingMode.AutoTiles)
            {
                Undo.RecordObject(m_target, "Autotile Change");
                m_target.PaintAutoTile((int) hitCoord.x, (int) hitCoord.y, m_target.tilemapLayerList[m_target.layerIndex].layoutIndex, m_target.layerIndex);
            }
            
            // Erasing the auto tile and regular tile
            if (m_target.toolIndex == 4)
            {
                Undo.RecordObject(m_target, "Autotile Change");
                m_target.EraseAutoTile((int) hitCoord.x, (int) hitCoord.y, m_target.tilemapLayerList[m_target.layerIndex].layoutIndex, m_target.layerIndex);
            }
        }
    }
    
    /// <summary>
    /// Create the auto tiles reorderable list.
    /// </summary>
    private ReorderableList CreateAutoTileList (SerializedProperty list)
    {
        return new ReorderableList(serializedObject, list, false, true, true, true)
        {
            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Initialize
                var element = m_autoTileDictionary[m_target.layerIndex].serializedProperty.GetArrayElementAtIndex(index);
                var id = element.FindPropertyRelative("id");
                var isPicked = element.FindPropertyRelative("isPicked");
                var layoutButtonRect = new Rect(rect.x, rect.y, 32, 32);
                var layoutImageRect = new Rect(rect.x, rect.y, 32, 32);
                var layoutName = element.FindPropertyRelative("name");
                var layoutIconTexture = blobAutoTileLayout.temporaryTileTextureArray[0];
                
                // Get the icon texture
                if (m_target.tilemapLayerList[m_target.layerIndex].autoTileList.Count > 0)
                {
                    if (m_target.tilemapLayerList[m_target.layerIndex].autoTileList[index].isPicked[0])
                        layoutIconTexture = m_target.tilemapLayerList[m_target.layerIndex].tilePalette.temporaryTileTextureArray[id.GetArrayElementAtIndex(0).intValue];
                }
                
                // Draw the automatic layout button
                if (GUI.Button(new Rect(rect.x, rect.y + 2, 48, 16), m_guiContent[12]))
                {
                    for (var i = 0; i < id.arraySize; i++)
                    {
                        if (i + m_target.tileIndex < m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount)
                        {
                            id.GetArrayElementAtIndex(i).intValue = i + m_target.tileIndex;
                            isPicked.GetArrayElementAtIndex(i).boolValue = true;
                        }
                    }
                }
                
                // Draw the layout name
                layoutName.stringValue = EditorGUI.TextField(new Rect(rect.x + 53, rect.y + 2, rect.width - 53, 16), layoutName.stringValue);
                
                // Draw the main icon texture
                GUI.DrawTexture(new Rect(rect.x, rect.y + 20, 48, 48), layoutIconTexture, ScaleMode.StretchToFill, true, 0);
                
                // Draw the auto tile layout selector
                m_autoTileScrollPos = GUI.BeginScrollView(new Rect(rect.x + 53, rect.y + 20, rect.width - 53, 48), m_autoTileScrollPos, new Rect(rect.x, rect.y, 1598, 34));
                for (var i = 0; i < id.arraySize; i++)
                {
                    // If the pickup button was pressed
                    if (GUI.Button(layoutButtonRect, GUIContent.none))
                    {
                        if (m_target.tileIndex < m_target.tilemapLayerList[m_target.layerIndex].tilePalette.tilesCount)
                        { 
                            // Pickup the current tile selected in the grid
                            id.GetArrayElementAtIndex(i).intValue = m_target.tileIndex;
                            isPicked.GetArrayElementAtIndex(i).boolValue = !isPicked.GetArrayElementAtIndex(i).boolValue;
                        }
                        else
                        {
                            id.GetArrayElementAtIndex(i).intValue = 0;
                            isPicked.GetArrayElementAtIndex(i).boolValue = false;
                        }
                    }
                    
                    // Set the rect to the next slot
                    layoutImageRect.x += 2;
                    layoutImageRect.y += 2;
                    layoutImageRect.width -= 4;
                    layoutImageRect.height -= 4;
                    
                    // Draw the layout preview texture
                    if (isPicked.GetArrayElementAtIndex(i).boolValue)
                        layoutIconTexture = m_target.tilemapLayerList[m_target.layerIndex].tilePalette.temporaryTileTextureArray[id.GetArrayElementAtIndex(i).intValue];
                    else
                        layoutIconTexture = blobAutoTileLayout.temporaryTileTextureArray[i];
                    
                    GUI.DrawTexture(layoutImageRect, layoutIconTexture, ScaleMode.StretchToFill, true, 0);
                    layoutButtonRect.x += 34;
                    layoutImageRect = layoutButtonRect;
                }
                GUI.EndScrollView();
            },

            onAddCallback = (ReorderableList l) =>
            {
                var index = l.serializedProperty.arraySize;
                l.serializedProperty.arraySize++;

                var element = l.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("isPicked").arraySize = 47;
                element.FindPropertyRelative("id").arraySize = 47;
                element.FindPropertyRelative("name").stringValue = "Auto Tile Layout " + index;
            },

            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, m_guiContent[11], EditorStyles.boldLabel);
            },

            elementHeightCallback = (int index) => 72,

            drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    GUI.Box(new Rect(rect.x + 2, rect.y - 1, rect.width - 4, rect.height + 1), "", "selectionRect");
            }
        };
    }
    
    /// <summary>
    /// Check if there is a change in the settings.
    /// Use to enable and disable the paint tools.
    /// </summary>
    private bool CanPaint()
    {
        if (m_target.tilemapData.settingsState.layersCount != m_target.tilemapLayerList.Count) return false;
        if (m_target.tilemapData.settingsState.tilemapWidth != (int) m_gridMapSize.x) return false;
        if (m_target.tilemapData.settingsState.tilemapHeight != (int) m_gridMapSize.y) return false;
        if (m_target.tilemapData.settingsState.tilesetSize != m_tilesetSize.intValue) return false;
        
        if (m_target.tilemapData.settingsState.layersCount == m_target.tilemapLayerList.Count)
        {
            for (int i = 0; i < m_target.tilemapLayerList.Count; i++)
            {
                if (m_target.tilemapData.settingsState.tilePalette[i] != m_target.tilemapLayerList[i].tilePalette)
                    return false;
                if (m_target.tilemapData.settingsState.tilesCount[i] != m_target.tilemapLayerList[i].tilePalette.tilesCount)
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if the user changed the alpha intensity of some layer.
    /// </summary>
    private bool NeedUpdateLayerIntensity()
    {
        if (m_target.tilemapData.layerIntensity.Length == m_target.tilemapLayerList.Count)
        {
            for (int i = 0; i < m_target.tilemapLayerList.Count; i++)
            {
                if (Math.Abs(m_target.tilemapData.layerIntensity[i] - m_target.tilemapLayerList[i].intensity) > 0)
                    return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Reset the auto tiles array map.
    /// </summary>
    private void ResetAutoTileArrayMap(SerializedProperty array)
    {
        array.arraySize = (int) (m_target.gridMapSize.x * m_target.gridMapSize.y);
        for (int i = 0; i < array.arraySize; i++)
        {
            //array.GetArrayElementAtIndex(i).intValue = -1;
            array.GetArrayElementAtIndex(i).boolValue = false;
        }
    }
    
    /// <summary>
    /// Gets all elements of the auto tile list and returns the string names.
    /// </summary>
    /// <returns></returns>
    private string[] GetAutoTileElements()
    {
        string[] temp = new string[m_target.tilemapLayerList[m_target.layerIndex].autoTileList.Count];
        
        for (int i = 0; i < temp.Length; i++)
        {
            temp[i] = m_target.tilemapLayerList[m_target.layerIndex].autoTileList[i].name;
        }

        return temp;
    }
}