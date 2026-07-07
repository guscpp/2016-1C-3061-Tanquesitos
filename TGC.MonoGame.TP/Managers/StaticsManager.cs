using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Decorations;
using TGC.MonoGame.TP.Collisions;
using Static = TGC.MonoGame.TP.Models.Decorations.Static;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

public class StaticsManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //PATHS
    private string[] DecorationModelPaths =
    {
        "decoraciones/arbol_muerto_1",
        "decoraciones/arbol_muerto_2",
        "decoraciones/cactus_1",
        "decoraciones/cactus_2",
        "decoraciones/cactus_3",
        "decoraciones/pozo",
        "decoraciones/roca_1",
        "decoraciones/roca_2",
        "decoraciones/roca_3"
    };
    //SPAWRATES
    private readonly float[] _decorationSpawrates =
    {
        0.03f,  // arbol_muerto_1
        0.03f,  // arbol_muerto_2
        0.10f,  // cactus_1    
        0.10f,  // cactus_2        
        0.10f,  // cactus_3       
        0.04f,  // pozo
        0.15f,  // roca_1        
        0.13f,  // roca_2         
        0.03f,  // roca_3         
    };
    private const int NumberOfAssets = 200; 
    private int NumberOfDecorations => NumberOfAssets - 15;
    public List<Static> _decorationModels = new();
    public List<Vector3> _houses = new();
    private Terrain _terrain;
    private readonly Random _random = new();  
    private GraphicsDevice _graphicsDevice;
    
    // Diccionario que agrupa matrices por ruta de modelo
    private Dictionary<string, List<Matrix>> _instancedMatrices = new();
    // Diccionario que guarda los grupos de instanciado ya inicializados
    private Dictionary<string, InstancedDecorationGroup> _decorationGroups = new();
    public Dictionary<StaticHandle, Static> StaticsByHandle { get; private set; } = new();

    public StaticsManager(Terrain terrain, List<Vector3> houses, GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _terrain = terrain;
        _houses = houses;
    }

    public void Initialize()
    {
        var decorationModelPositions = GetValidDecorationPositions();
        for (int i = 0; i < NumberOfDecorations; i++)
        {
            _decorationModels.Add(GetDecoration(decorationModelPositions[i]));
        } 
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");
        var sharedTexture = content.Load<Texture2D>("Textures/paleta_256x512");

        foreach (var asset in _decorationModels)
        {
            asset.LoadContent(content, simulation, effect);
            StaticsByHandle[asset.StaticHandle] = asset;
            
            string modelPath = asset.ModelPath;
            if (!_instancedMatrices.ContainsKey(modelPath))
                _instancedMatrices[modelPath] = new List<Matrix>();
                
            _instancedMatrices[modelPath].Add(asset.WorldMatrix);
        }

        foreach (var entry in _instancedMatrices)
        {
            var model = content.Load<Model>(ContentFolder3D + entry.Key);
            var normalOffset = CalculateNormalOffsetScale(model);
            _decorationGroups[entry.Key] = new InstancedDecorationGroup(model, normalOffset, entry.Value, _graphicsDevice, sharedTexture, effect);
        }
    }

    private float CalculateNormalOffsetScale(Model model)
    {
        var bbox = BoundingVolumesUtils.CreateBoundingBox(model);
        var dimensions = bbox.Max - bbox.Min;
        var objectSize = Math.Max(dimensions.X, Math.Max(dimensions.Y, dimensions.Z));
        return MathHelper.Clamp(objectSize * 0.02f, 0.03f, 0.6f);
    }

    public void Update(BoundingFrustum CameraFrustum)
    {
        var visibleModels = new Dictionary<string, List<Matrix>>();
        foreach(var staticDecoration in _decorationModels)
        {
            var boundingBox = staticDecoration.BoundingBox;
            if(CameraFrustum.Intersects(boundingBox))
            {
                if (!visibleModels.TryGetValue(staticDecoration.ModelPath, out var list))
                {
                    list = new List<Matrix>();
                    visibleModels[staticDecoration.ModelPath] = list;
                }
                list.Add(staticDecoration.WorldMatrix);
            }
        }
        // se actualiza con las instancias visibles
        foreach (var group in _decorationGroups)
        {
            if (visibleModels.TryGetValue(group.Key, out var visibleMatrices))
                group.Value.SetVisibleInstances(visibleMatrices);
            else
                group.Value.SetVisibleInstances(new List<Matrix>()); // nada visible de ese modelo
        }

        int totalVisible = visibleModels.Values.Sum(l => l.Count);
        //Console.WriteLine($"Estaticos Visibles: {totalVisible} / {_decorationModels.Count}");
    }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var group in _decorationGroups.Values)
        {
            group.Draw(view, projection);
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        Console.WriteLine($"DrawDepth grupos: {_decorationGroups.Count}");

        foreach (var group in _decorationGroups)
        {
            // 1. Obtenemos la lista original con TODAS las matrices (sin culling)
            var fullList = _instancedMatrices[group.Key];

            // 2. Restauramos el buffer en la GPU para que recupere el 100% de los objetos
            group.Value.SetVisibleInstances(fullList);

            // 3. Dibujamos el shadow map completo
            group.Value.DrawDepth(lightViewProjection);
        }
    }

    private Vector3 GetRandomPosition()
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = _random.NextSingle() * horizontalRange + minHorizontal;
        var z = _random.NextSingle() * horizontalRange + minHorizontal;
        return new Vector3(x, _terrain.GetHeight(x, z), z);
    }

    public Static GetDecoration(Vector3 position)
    {
        _ = position + Vector3.Up * GameConfig.Assets.DynamicSpawnOffset;
        Vector3 rocaPos = position + Vector3.Up * 1.0f;
        var path = GetRandomAssetPath();
        return path switch
        {
            var p when p.Contains("arbol")      => new Tree(position, path),
            var p when p.Contains("cactus")     => new Cactus(position, path),
            var p when p.Contains("roca")       => new Rock(rocaPos, path),
            var p when p.Contains("pozo")       => new Pozo(position, path),
            _                                   => new Static(position, path)
        };
    }

    private string GetRandomAssetPath()
    {
        var aux = _random.NextSingle();
        var acum = 0f;
        var index =  _random.Next(0, DecorationModelPaths.Length);
        
        for(int i=0; i<DecorationModelPaths.Length; i++)
        {
            acum += _decorationSpawrates[i];
            if(aux<acum)
                return DecorationModelPaths[i];   
        }

        return DecorationModelPaths[index];
    }
    
    private List<Vector3> GetValidDecorationPositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceToHouses = 30f;
        var minDistanceBetween = 10f;
        var minDistanceToSpawn = 20f;
        float margin = 8f;
        float playAreaLimit = _terrain.WidthUnits - margin;
  
        for(int i = 0; i < NumberOfDecorations; i++)
        {
            Vector3 candidate;
            bool valid;
            var spawnPoint = new Vector3(0, _terrain.GetHeight(0, 0), 0);
            do
            {
                candidate = GetRandomPosition();
                valid = true;

                if(Math.Abs(candidate.X) >= playAreaLimit || Math.Abs(candidate.Z) >= playAreaLimit)
                {
                    valid = false;
                    continue;
                }
                if(IsTooNearToAHouse(candidate, minDistanceToHouses) ||
                    Vector3.Distance(candidate, spawnPoint) < minDistanceToSpawn)
                {
                    valid = false;
                    continue;
                }
                for(int j = 0; j < i; j++)
                {
                    if(Vector3.Distance(candidate, positions[j]) < minDistanceBetween)
                    {
                        valid = false;
                        break;
                    }
                }

            } while(!valid);

            positions.Add(candidate);
        }

        return positions;
    }

    private bool IsTooNearToAHouse(Vector3 position, float minDistance)
    {
        for(int i=0; i<_houses.Count(); i++)
        {
            if(Vector3.Distance(position, _houses[i]) < minDistance)
                return true;
        }
        return false;
    }

    public List<Vector3> GetDecorations()
    {
        return _decorationModels.Select(decoration => decoration.Position).ToList();
    }

    public void Dispose()
    {
        if (_decorationGroups != null)
        {
            foreach (var group in _decorationGroups.Values)
            {
                group?.Dispose();
            }
            _decorationGroups.Clear();
        }
    }
}
