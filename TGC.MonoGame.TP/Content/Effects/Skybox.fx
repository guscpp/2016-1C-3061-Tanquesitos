// ============================================================================
// =    Skybox.fx - Renderiza un cubemap como fondo infinitamente lejano      =
// ============================================================================

float4x4 WorldViewProjection;
TextureCube SkyboxTexture;

samplerCUBE SkyboxSampler = sampler_state
{
    Texture = <SkyboxTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
};

struct VSInput
{
    float4 Position : POSITION0;
};

struct VSOutput
{
    float4 Position : SV_POSITION0;
    float3 TexCoord : TEXCOORD0;
};

VSOutput VertexShaderFunction(VSInput input)
{
    VSOutput output;
    
    // fuerza Z = W, haciendo que el depth buffer siempre lea 1.0, entonces
    // todo lo demas se dibuja delante.
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = float4(pos.xy, pos.w, pos.w);
    
    // usar la posicion del vertice como direccion de muestreo del cubemap
    output.TexCoord = input.Position.xyz;
    
    return output;
}

float4 PixelShaderFunction(VSOutput input) : SV_TARGET0
{
    return texCUBE(SkyboxSampler, normalize(input.TexCoord));
}

technique Skybox
{
    pass P0
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}