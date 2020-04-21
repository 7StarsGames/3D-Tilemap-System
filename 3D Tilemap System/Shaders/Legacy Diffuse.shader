Shader "3D Tilemap System/Legacy Diffuse"
{
    Properties
    {
        _TilemapTexture ("Tilemap Texture3D", 3D) = "black" {}
        _TilesetTexture ("Tileset Texture3D", 3D) = "black" {}
        _LayerArrayTexture ("Layer Array Texture1D", 2D) = "black" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        
        #pragma surface surf Lambert vertex:vert
        
        uniform sampler2D _LayerArrayTexture;
        uniform sampler3D _TilemapTexture;
        uniform sampler3D _TilesetTexture;
        uniform int2      _GridMapSize;
        uniform int       _LayersCount;
        uniform int       _TilesCount;
        
        struct Input
        {
            float4 vertex     : SV_POSITION;
            float2 tilemap_uv : TEXCOORD0;
            float2 tileset_uv : TEXCOORD1;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.tilemap_uv = v.texcoord.xy;
            o.tileset_uv = v.texcoord.xy * _GridMapSize.xy;
        }
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 layerData = float2(0, 0);
            float  tilemap = 0;
            fixed4 tileset = fixed4(0, 0, 0, 0);
            fixed4 outputColor = tex3D(_TilesetTexture, float3(IN.tileset_uv, 0));
            float  index = 0;
            float previousLayerCount = 0;

            for (int i = 0; i < _LayersCount; i++)
            {
                // Get layer data
                // layerData.x = The number of tiles in that layer
                // layerData.y = The alhpa intensity of that layer
                layerData = tex2D(_LayerArrayTexture, float2(1.0 / (_LayersCount - 1.0) * i, 0)).rg;

                // Get the tilemap data
                // Each pixel value of the 3d tilemap texture stores the slice number that the 3d tileset texture should render
                tilemap = tex3D(_TilemapTexture, float3(IN.tilemap_uv, 1.0 / (_LayersCount - 1) * i)).r * 255;

                // Compute the right slice index for current layer
                index = tilemap + previousLayerCount * 255;
                previousLayerCount += layerData.x;

                tileset = tex3D(_TilesetTexture, float3(IN.tileset_uv, 1.0 / (_TilesCount - 1) * index));
                outputColor = lerp(outputColor, tileset, layerData.y * tileset.a);
            }

            o.Albedo = outputColor.rgb;
            o.Alpha = outputColor.a;
        }
        
        ENDCG
    }
    
    Fallback "Legacy Shaders/VertexLit"
}