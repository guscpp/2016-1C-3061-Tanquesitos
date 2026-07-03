using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TGC.MonoGame.TP.Gizmos;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using FuelBarrel = TGC.MonoGame.TP.Models.Decorations.FuelBarrel;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

public class BarrelsManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    public List<FuelBarrel> _fuelBarrels = new();
    public List<Vector3> _decorationModels = new();
    public List<Vector3> _houses = new();

    private Terrain _terrain;
    private readonly Random _random = new();
    private List<Vector3> _barrelsPositionCache = new List<Vector3>(GameConfig.FuelBarrel.SpawnCount);

    public BarrelsManager(Terrain terrain, List<Vector3> decorationModels, List<Vector3> houses)
    {
        _terrain = terrain;
        _decorationModels = decorationModels;
        _houses = houses;
    }

    public void Initialize() 
    { 
        float minDistToDecorations = 8f;
        float minDistToHouses = 20f;
        float minDistToBarrels = 6f;
        float margin = 8f;
        float playAreaLimit = _terrain.WidthUnits - margin;

        for (int i = 0; i < GameConfig.FuelBarrel.SpawnCount; i++)
        {
            Vector3 pos;
            bool valid;
            do
            {
                pos = GetRandomPosition();
                valid = true;
                if(Math.Abs(pos.X) >= playAreaLimit || Math.Abs(pos.Z) >= playAreaLimit)
                {
                    valid = false;
                    continue;
                }

                if (IsTooNearToAHouse(pos, minDistToHouses)) { valid = false; continue; }

                foreach (var dec in _decorationModels)
                {
                    if (Vector3.Distance(pos, dec) < minDistToDecorations) { valid = false; break; }
                }
                if (!valid) continue;

                foreach (var barrel in _fuelBarrels)
                {
                    if (Vector3.Distance(pos, barrel.Position) < minDistToBarrels) { valid = false; break; }
                }
            } while (!valid);

            pos.Y += GameConfig.FuelBarrel.Height / 2f;

            _fuelBarrels.Add(new FuelBarrel(pos));
        }
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");

        foreach (var barrel in _fuelBarrels)
        {
            barrel.LoadContent(content, simulation, effect);
        }

    }

    public void Reset(Simulation simulation)
    {
        foreach(var barrel in _fuelBarrels)
            barrel.ResetBarrel(simulation);
    }

    public void Update(float dt) { 
        foreach (var barrel in _fuelBarrels)
        {
            barrel.UpdateRecharge(dt);
        }
    }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation, BoundingFrustum CameraFrustum)
    {
        int totalVisible = 0;
        foreach (var barrel in _fuelBarrels)
        {
            if (!barrel.IsCollected && CameraFrustum.Intersects(barrel.BoundingBox)) {barrel.Draw(view, projection); totalVisible++;}
        }
        //Console.WriteLine($"Casas Visibles: {totalVisible} / {_fuelBarrels.Count}");
    }

    public void DrawDepth(Matrix lightViewProjection, BoundingFrustum CameraFrustum)
    {
        foreach (var barrel in _fuelBarrels)
        {
            if (!barrel.IsCollected && CameraFrustum.Intersects(barrel.BoundingBox)) barrel.DrawDepth(lightViewProjection);
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

    public List<Vector3> GetBarrelsPositions()
    {
        _barrelsPositionCache.Clear();
        foreach(var barrel in _fuelBarrels)
            if(!barrel.IsCollected) _barrelsPositionCache.Add(barrel.Position);
        return _barrelsPositionCache;
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

}

