struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};
v2f vert (appdata v)
{
    v2f o;
    o.vertex = v.vertex;
    
    o.vertex.w = 1.0;
    #if UNITY_REVERSED_Z
    o.vertex.z = 0.001;
    #else
    o.vertex.z = 0.999;
    #endif
    
    o.uv = v.uv; 
    
    return o;
}			