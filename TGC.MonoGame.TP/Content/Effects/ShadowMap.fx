#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float4x4 WorldViewProjection;
float4x4 LightViewProjection;

float3 lightPosition;
float2 shadowMapSize;
float3 DiffuseColor;

float normalOffsetScale;

static const float modulatedEpsilon = 0.003;
static const float maxEpsilon = 0.0025;

int IsDeformable;
float ImpactRadius;
float4 Impacts[6];

float3 EyePosition;
float Shininess;
float3 LightColor;
float3 AmbientColor;

float TrackOffset;

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture shadowMapStatic;
sampler2D shadowMapStaticSampler = sampler_state {
    Texture = (shadowMapStatic);
    MinFilter = Point;
	MagFilter = Point; 
	MipFilter = Point;
    AddressU = Clamp; 
	AddressV = Clamp;
};

texture shadowMapDynamic;
sampler2D shadowMapDynamicSampler = sampler_state {
    Texture = (shadowMapDynamic);
    MinFilter = Point; 
	MagFilter = Point; 
	MipFilter = Point;
    AddressU = Clamp; 
	AddressV = Clamp;
};

float3 ApplyDeformation(float3 worldPos, float3 worldNormal)
{
    if (IsDeformable > 0)
    {
        float totalDeformation = 0;
        for (int i = 0; i < 6; i++)
        {
            if (Impacts[i].w > 0)
            {
                float dist = distance(worldPos, Impacts[i].xyz);
                if (dist < ImpactRadius)
                {
                    float factor = 1.0 - (dist / ImpactRadius);
                    totalDeformation += factor * Impacts[i].w;
                }
            }
        }
        worldPos -= worldNormal * totalDeformation;
    }
    return worldPos;
}

struct DepthPassVertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
};

struct DepthPassVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 ScreenSpacePosition : TEXCOORD1;
};

struct ShadowedVertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
	float2 TextureCoordinates : TEXCOORD0;
};

struct ShadowedVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TextureCoordinates : TEXCOORD0;
	float4 WorldSpacePosition : TEXCOORD1;
	float4 LightSpacePosition : TEXCOORD2;
    float4 Normal : TEXCOORD3;
};

struct InstanceVertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL;
    float2 TextureCoordinates : TEXCOORD0;
    
    float4 InstanceWorld1 : TEXCOORD1;
    float4 InstanceWorld2 : TEXCOORD2;
    float4 InstanceWorld3 : TEXCOORD3;
    float4 InstanceWorld4 : TEXCOORD4;
};

DepthPassVertexShaderOutput DepthVS(in DepthPassVertexShaderInput input)
{
	DepthPassVertexShaderOutput output;

    float4 worldPos = mul(input.Position, World);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)World));
    worldPos.xyz = ApplyDeformation(worldPos.xyz, worldNormal);

    float4 offsetWorldPos = worldPos + float4(worldNormal * normalOffsetScale, 0);

    output.Position = mul(offsetWorldPos, LightViewProjection);
	output.ScreenSpacePosition = output.Position;
	
	return output;
}

float4 DepthPS(in DepthPassVertexShaderOutput input) : COLOR
{
	float depth = input.ScreenSpacePosition.z / input.ScreenSpacePosition.w;
	return float4(depth, depth, depth, 1.0);
}

ShadowedVertexShaderOutput MainVS(in ShadowedVertexShaderInput input)
{
    ShadowedVertexShaderOutput output;

    float4 worldPos = mul(input.Position, World);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)World));
    worldPos.xyz = ApplyDeformation(worldPos.xyz, worldNormal);

    output.WorldSpacePosition = worldPos;

    float4 viewPosition = mul(worldPos, View);
    output.Position = mul(viewPosition, Projection);

    output.TextureCoordinates = input.TextureCoordinates;
    output.TextureCoordinates.y += TrackOffset;

    float4 offsetWorldPos = worldPos + float4(worldNormal * normalOffsetScale, 0);
    output.LightSpacePosition = mul(offsetWorldPos, LightViewProjection);

    output.Normal = float4(worldNormal, 1);

    return output;
}

ShadowedVertexShaderOutput InstancedVS(in InstanceVertexShaderInput input)
{
    ShadowedVertexShaderOutput output;
    
    float4x4 instanceWorld = float4x4(
        input.InstanceWorld1,
        input.InstanceWorld2,
        input.InstanceWorld3,
        input.InstanceWorld4
    );

    float4 worldPos = mul(input.Position, instanceWorld);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)instanceWorld));

    worldPos.xyz = ApplyDeformation(worldPos.xyz, worldNormal);

    output.WorldSpacePosition = worldPos;

    float4 viewPosition = mul(worldPos, View);
    output.Position = mul(viewPosition, Projection);

    output.TextureCoordinates = input.TextureCoordinates;

    float4 offsetWorldPos = worldPos + float4(worldNormal * normalOffsetScale, 0);
    output.LightSpacePosition = mul(offsetWorldPos, LightViewProjection);

    output.Normal = float4(worldNormal, 1);

    return output;
}

float4 ShadowedPCFPS(in ShadowedVertexShaderOutput input) : COLOR
{
	float3 lightSpacePosition = input.LightSpacePosition.xyz / input.LightSpacePosition.w;
	float2 shadowMapTextureCoordinates = 0.5 * lightSpacePosition.xy + float2(0.5, 0.5);
	shadowMapTextureCoordinates.y = 1.0f - shadowMapTextureCoordinates.y;

	float3 normal = normalize(input.Normal.rgb);
	float3 lightDirection = normalize(lightPosition - input.WorldSpacePosition.xyz);
	float inclinationBias = max(modulatedEpsilon * (1.0 - dot(normal, lightDirection)), maxEpsilon);

	float notInStaticShadow = 0.0;
    float notInDynamicShadow = 0.0;
	float2 texelSize = 1.0 / shadowMapSize;

	for (int x = -2; x <= 2; x++)
	{
		for (int y = -2; y <= 2; y++)
		{
			float2 offset = float2(x, y) * texelSize;
            float staticDepth = tex2D(shadowMapStaticSampler, shadowMapTextureCoordinates + offset).r + inclinationBias;
            notInStaticShadow += step(lightSpacePosition.z, staticDepth);
            float dynamicDepth = tex2D(shadowMapDynamicSampler, shadowMapTextureCoordinates + offset).r + inclinationBias;
            notInDynamicShadow += step(lightSpacePosition.z, dynamicDepth);
		}
	}

    notInStaticShadow /= 25.0;
    notInDynamicShadow /= 25.0;
    float factorSombraFinal = notInStaticShadow * notInDynamicShadow;

    float4 texColor = tex2D(textureSampler, input.TextureCoordinates);
    float3 surfaceColor = texColor.rgb * DiffuseColor;

    float3 ambient = AmbientColor * surfaceColor;

    float diffuseFactor = saturate(dot(normal, lightDirection));
    float3 diffuse = diffuseFactor * LightColor * surfaceColor * factorSombraFinal;

    float3 viewDir = normalize(EyePosition - input.WorldSpacePosition.xyz);
    float3 halfVec = normalize(lightDirection + viewDir);
    float3 specular = 0;
    if (diffuseFactor > 0)
    {
        float specFactor = pow(max(dot(normal, halfVec), 0), Shininess);
        specular = specFactor * LightColor * factorSombraFinal;
    }

    float3 finalColor = ambient + diffuse + specular;

    return float4(finalColor, texColor.a);
}

DepthPassVertexShaderOutput DepthInstancedVS(DepthPassVertexShaderInput input, InstanceVertexShaderInput instanceInput)
{
    DepthPassVertexShaderOutput output;

    float4x4 instanceWorld = float4x4(
        instanceInput.InstanceWorld1,
        instanceInput.InstanceWorld2,
        instanceInput.InstanceWorld3,
        instanceInput.InstanceWorld4
    );

    float3 worldPos = mul(input.Position, instanceWorld).xyz;
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)instanceWorld));
    worldPos = ApplyDeformation(worldPos, worldNormal);

    float4 offsetWorldPos = float4(worldPos, 1.0) + float4(worldNormal * normalOffsetScale, 0);
    output.Position = mul(offsetWorldPos, LightViewProjection);
    output.ScreenSpacePosition = output.Position;

    return output;
}

// TECNICA DE PARTICULAS
texture ParticleTexture;
sampler2D particleSampler = sampler_state
{
    Texture = <ParticleTexture>;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct ParticleVSInput
{
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct ParticleVSOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

ParticleVSOutput ParticleVS(ParticleVSInput input)
{
    ParticleVSOutput output;
    float4 viewPos = mul(input.Position, View);
    output.Position = mul(viewPos, Projection);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 ParticlePS(ParticleVSOutput input) : COLOR0
{
    float4 texColor = tex2D(particleSampler, input.TexCoord);
    return float4(texColor.rgb * input.Color.rgb, texColor.a * input.Color.a);
}

technique Particles
{
    pass Pass0
    {
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        ZWriteEnable = false;

        VertexShader = compile VS_SHADERMODEL ParticleVS();
        PixelShader  = compile PS_SHADERMODEL ParticlePS();
    }
};


technique DepthPass
{
	pass Pass0
	{
		VertexShader = compile VS_SHADERMODEL DepthVS();
		PixelShader = compile PS_SHADERMODEL DepthPS();
	}
};

technique DrawShadowedHibrido {
    pass Pass0 {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ShadowedPCFPS();
    }
};

technique DrawInstanced
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL InstancedVS();
        PixelShader = compile PS_SHADERMODEL ShadowedPCFPS();
    }
};

technique DepthPassInstanced
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL DepthInstancedVS();
        PixelShader = compile PS_SHADERMODEL DepthPS();
    }
};