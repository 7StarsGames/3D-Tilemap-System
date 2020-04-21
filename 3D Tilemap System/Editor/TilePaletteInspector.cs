using UnityEngine;
using UnityEditor;
using UnityEngine.TilemapSystem3D;

[CustomEditor(typeof(TilePalette))]
public class TilePaletteInspector : Editor
{
    // The target script
    private TilePalette m_target;
    
    // Editor only
    public Texture2D emptyIconTexture;
    private readonly Color m_noColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    private readonly Color m_greenColor = new Color(0.85f, 1.0f, 0.85f);
    private readonly Color m_redColor = new Color(1.0f, 0.75f, 0.75f);
    private readonly int[] m_tileSizeArray = new[] {16, 24, 32, 48, 64, 96, 128};
    private readonly GUIContent[] m_tileSizeContentArray = new GUIContent[]
    {
        new GUIContent("16x16", "Select the proper resolution for your tiles."),
        new GUIContent("24x24", "Select the proper resolution for your tiles."),
        new GUIContent("32x32", "Select the proper resolution for your tiles."),
        new GUIContent("48x48", "Select the proper resolution for your tiles."),
        new GUIContent("64x64", "Select the proper resolution for your tiles."),
        new GUIContent("96x96", "Select the proper resolution for your tiles."),
        new GUIContent("128x128", "Select the proper resolution for your tiles.")
        //new GUIContent("192x192", "The resolution of each tile."),
        //new GUIContent("256x256", "The resolution of each tile.")
    };

    // Scroll view
    private Vector2 m_scrollPos;
    private Rect m_controlRect;
    private Texture2D m_tileTexture;
    
    // GUIContents
    private readonly GUIContent[] m_guiContent = new[]
    {
        new GUIContent("Tile Size", "The tiles resolution."),
        new GUIContent("Atlas Texture:", "The source tileset texture with all the tiles that should be converted and added to this palette."),
        new GUIContent("Grid Offset", "The offset between each tile in the atlas texture."),
        new GUIContent("Convert Atlas to Tiles Temporary", "Convert the atlas texture to single tiles temporary. By pressing this, each tile texture will be stored in memory temporarily, so build the 3D Tileset Data to save it permanently as a Texture3D."),
        new GUIContent("Convert Atlas to Tiles", "Convert the atlas texture to single tile assets. By pressing this, each tile texture will be saved separately as .asset into this Palette"),
        new GUIContent("Remove Tiles", "Remove all the tiles from this Palette"),
        new GUIContent("Reset", "Reset this Palette to the default settings."),
        new GUIContent("Extract Number", "The max number of tiles in the atlas texture that you want to extract and convert to tiles."),
        new GUIContent("", "Make sure the import settings option (Non-Power of Two) of the atlas texture is set to 'None'. Otherwise this may cause some issues when extracting the tiles.")
    };
    
    // Temporary
    private int m_tileSize = 32;
    
    // Serialized properties
    private SerializedProperty m_gridOffset;
    private SerializedProperty m_atlasTexture;
    private SerializedProperty m_extractNumber;

    private void OnEnable()
    {
        // Get Target
        m_target = (TilePalette) target;
        
        // Find the serialized properties
        m_gridOffset = serializedObject.FindProperty("gridOffset");
        m_atlasTexture = serializedObject.FindProperty("atlasTexture");
        m_extractNumber = serializedObject.FindProperty("extractNumber");
    }

    public override void OnInspectorGUI()
    {
        // Start custom Inspector
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        
        // Reset button
        m_controlRect = EditorGUILayout.GetControlRect();
        if (GUI.Button(new Rect(m_controlRect.width - 73, -26, 45, 18), m_guiContent[6]))
            m_target.ResetAsset();
        GUILayout.Space(-20);
        
        // Settings section
        EditorGUILayout.LabelField("Settings", (GUIStyle)"DD Background"); // (GUIStyle)"SelectionRect", (GUIStyle)"dockareaStandalone"
        GUILayout.Space(-4);
        
        // Tile size popup
        EditorGUILayout.BeginHorizontal("Box");
        m_tileSize = EditorGUILayout.IntPopup(m_guiContent[0], m_target.tileSize, m_tileSizeContentArray, m_tileSizeArray);
        EditorGUILayout.EndHorizontal();
        
        // Conversion section
        EditorGUILayout.LabelField("Conversion", (GUIStyle)"DD Background"); // (GUIStyle)"ObjectPickerResultsEven", (GUIStyle)"LODSliderRangeSelected"
        GUILayout.Space(-4);

        // Texture field for the diffuse tileset atlas
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.HelpBox(m_guiContent[8].tooltip, MessageType.Info);
        GUI.color = m_greenColor;
        if (!m_atlasTexture.objectReferenceValue) GUI.color = m_redColor;
        EditorGUILayout.PropertyField(m_atlasTexture, m_guiContent[1]);
        GUI.color = Color.white;
        
        // Grid offset
        EditorGUILayout.PropertyField(m_gridOffset, m_guiContent[2]);

        // Extract number
        EditorGUILayout.PropertyField(m_extractNumber, m_guiContent[7]);

        // Button to convert the atlas texture to separate tiles and save it as .asset file
        if (GUILayout.Button(m_guiContent[4]))
            m_target.ConvertAtlasToTiles();
        
        // Delete tile assets button
        if (GUILayout.Button(m_guiContent[5]))
            m_target.DeleteTiles();
        EditorGUILayout.EndVertical();
        
        // Tileset data section
        EditorGUILayout.LabelField("Tileset Data", (GUIStyle)"DD Background");
        GUILayout.Space(-4);
        
        // Build 3D tileset data
        EditorGUILayout.BeginVertical("Box");
        //if (GUILayout.Button(m_guiContent[6]))
        //    m_target.BuildDiffuseTilesetData();
        
        // Tile grid
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
        EditorGUILayout.LabelField("", GUILayout.Width(442));
        GUILayout.Space(-20);
        
        for (int vertical = 0; vertical < 256;)
        {
            m_controlRect = EditorGUILayout.GetControlRect(GUILayout.Width(24), GUILayout.Height(24));

            EditorGUILayout.BeginHorizontal();
            for (int horizontal = 0; horizontal < 16; horizontal++)
            {
                m_tileTexture = m_target.temporaryTileTextureArray[vertical] ? m_target.temporaryTileTextureArray[vertical] : emptyIconTexture;
                GUI.DrawTexture(m_controlRect, m_tileTexture, ScaleMode.StretchToFill, true, 0);
                m_controlRect.x += 28;
                vertical++;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Used Slots: " + m_target.tilesCount.ToString());
        GUILayout.FlexibleSpace();
        GUILayout.Label("Free Slots: " + (256 - m_target.tilesCount).ToString());
        EditorGUILayout.EndHorizontal();
        
        // End custom Inspector
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(m_target, "3D Tilemap System");
            serializedObject.ApplyModifiedProperties();
            m_target.tileSize = m_tileSize;
        }
    }
}