using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TGC.MonoGame.TP.Models.Decorations;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

public class DinamicsManager
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
        "decoraciones/barril",
        "decoraciones/caja",
        "decoraciones/carreta_1",
        "decoraciones/carreta_2",
        "decoraciones/escaleras",
        "decoraciones/planta_rodadora"
    };
    //SPAWRATES
    private readonly float[] _decorationSpawrates =
    {
        0.04f,  // barril  
        0.04f,  // caja
        0.03f,  // carreta_1
        0.03f,  // carreta_2
        0.03f,  // escaleras
        0.12f,  // planta_rodadora         
    };
    private const int NumberOfAssets = 200; 
    public List<Dinamic> _dynamicDecorations = new();
    private List<Vector3> _staticDecorations;
    public List<Vector3> _houses = new();
    private Terrain _terrain;
    private readonly Random _random = new();
    public Dictionary<BodyHandle, Dinamic> DynamicDecorationsByHandle { get; private set; } = new();

    public DinamicsManager(Terrain terrain, List<Vector3> staticDecorations, List<Vector3> houses)
    {
        _terrain = terrain;
        _staticDecorations = staticDecorations;
        _houses = houses;
    }

    public void Initialize()
    {
        var decorationModelPositions = GetValidDecorationPositions();
        for (int i = 0; i < NumberOfAssets; i++)
        {
            _dynamicDecorations.Add(GetDecoration(decorationModelPositions[i]));
        } 
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");

        foreach (var asset in _dynamicDecorations)
        {
            asset.LoadContent(content, simulation, effect);
            DynamicDecorationsByHandle[asset.bodyHandle] = asset;
        }
    }

    public void ResetDynamics(Simulation simulation)
    {
        foreach (var asset in _dynamicDecorations)
            if(asset is Dinamic dinamicAsset) simulation.Bodies.Remove(dinamicAsset.bodyHandle);
        _dynamicDecorations.Clear();
        DynamicDecorationsByHandle.Clear();
        Initialize();
        LoadContent(TGCGame.Instance.Content, simulation);
    }

    public void Update(Simulation simulation)
    {
        // Recorremos la lista de atrás hacia adelante para poder borrar elementos de forma segura
        //Recorro la lista de elementos decorativos (conforme los mato la lista disminuye)
        for (int i = _dynamicDecorations.Count - 1; i >= 0; i--)
        {
            //Tomo el asset
            var asset = _dynamicDecorations[i];

            // Reviso que sea dinamico, sino me importa poco
            if (asset is Dinamic dinamicAsset)
            {
                // Reviso si esta muerto
                if (dinamicAsset.IsDead)
                {
                    // Lo borro de bepu
                    simulation.Bodies.Remove(dinamicAsset.bodyHandle);

                    DynamicDecorationsByHandle.Remove(dinamicAsset.bodyHandle);

                    //Lo borro de la lista
                    _dynamicDecorations.RemoveAt(i);
                    continue; 
                }
            }

            // Si no esta muerto se actualiza normal
            asset.Update(simulation);
        }
    }

    public void Draw(Matrix view, Matrix projection, BoundingFrustum CameraFrustum)
    {
        int totalVisible = 0;
        foreach (var asset in _dynamicDecorations)
        {
            if(CameraFrustum.Intersects(asset.BoundingBox)) {
                asset.Draw(view, projection);
                totalVisible++;
            }
        }
        //Console.WriteLine($"Dinamicos Visibles: {totalVisible} / {NumberOfAssets}");
    }

    public void DrawDepth(Matrix lightViewProjection, BoundingFrustum CameraFrustum)
    {
        foreach (var asset in _dynamicDecorations)
        {
            if(CameraFrustum.Intersects(asset.BoundingBox))
                asset.DrawDepth(lightViewProjection);
        }
    }

    private Vector3 GetRandomPosition()
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = _random.NextSingle() * horizontalRange + minHorizontal;
        var z = _random.NextSingle() * horizontalRange + minHorizontal;
        return new Vector3(x, _terrain.GetHeight(x, z)+2, z);
    }

    public Dinamic GetDecoration(Vector3 position)
    {
        Vector3 dynamicPos = position + Vector3.Up * GameConfig.Assets.DynamicSpawnOffset;
        Vector3 rocaPos = position + Vector3.Up * 1.5f;
        var path = GetRandomAssetPath();
        return path switch
        {
            var p when p.Contains("barril")     => new Barrel(dynamicPos, path),
            var p when p.Contains("carreta")    => new Cart(dynamicPos, path),
            var p when p.Contains("planta")     => new Plant(dynamicPos, path),
            var p when p.Contains("caja")       => new WoodenBox(dynamicPos, path),
            var p when p.Contains("escaleras")  => new Stairs(dynamicPos, path),
            _                                   => new Dinamic(position, path)
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
  
        for(int i = 0; i < NumberOfAssets; i++)
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

                foreach (var staticAsset in _staticDecorations)
                {
                    if (Vector3.Distance(candidate, staticAsset) < minDistanceBetween)
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid) continue;

                foreach (var approvedPos in positions)
                {
                    if (Vector3.Distance(candidate, approvedPos) < minDistanceBetween)
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

    public List<Decoration> GetDecorations()
    {
        return [.. _dynamicDecorations];
    }
}

