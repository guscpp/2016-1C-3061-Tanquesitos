using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Decorations;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class HousesManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //PATHS
    private string[] HouseModelPaths =
    {
        "casas/casita_mediana",
        "casas/casita_pequeña",
        "casas/Large Building B",
        "casas/Medium Building B"
    };
    public const int NumberOfHouseModels = 15;
    public List<House> _houses = new();
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();  
    private GraphicsDevice _graphicsDevice;
    // Diccionario que agrupa matrices por ruta de modelo
    private Dictionary<string, List<Matrix>> _instancedMatrices = new();
    // Diccionario que guarda los grupos de instanciado ya inicializados
    private Dictionary<string, InstancedDecorationGroup> _houseGroups = new();
    public Dictionary<StaticHandle, House> HousesByHandle { get; private set; } = new();

    public HousesManager(Terrain terrain, GraphicsDevice graphicsDevice)
    {
        _terrain = terrain;
        _graphicsDevice = graphicsDevice;
    }

    public void Initialize()
    {
        var houseModelPositions = GetValidHousePositions();
        for (int i = 0; i < NumberOfHouseModels; i++)
        {
            _houses.Add(GetHouse(houseModelPositions[i]));
        }
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");
        var sharedTexture = content.Load<Texture2D>("Textures/paleta_256x512");

        foreach (var house in _houses)
        {
            house.LoadContent(content, simulation, effect);
            HousesByHandle[house.StaticHandle] = house;
            string modelPath = house.ModelPath; // Asegúrate de tener esta propiedad
            if (!_instancedMatrices.ContainsKey(modelPath))
                _instancedMatrices[modelPath] = new List<Matrix>();
                
            _instancedMatrices[modelPath].Add(house.WorldMatrix);
        }
        foreach (var entry in _instancedMatrices)
        {
            var model = content.Load<Model>(ContentFolder3D + entry.Key);
            _houseGroups[entry.Key] = new InstancedDecorationGroup(model, 0.6f, entry.Value, _graphicsDevice, sharedTexture, effect);
        }
    }

    public void Update(BoundingFrustum CameraFrustum, Gizmo gizmos, Simulation simulation) 
    { 
        var visibleModels = new Dictionary<string, List<Matrix>>();
        foreach(var house in _houses)
        {
            var houseBoundingBox = house.BoundingBox;
            if(CameraFrustum.Intersects(houseBoundingBox))
            {
                if (!visibleModels.TryGetValue(house.ModelPath, out var list))
                {
                    list = new List<Matrix>();
                    visibleModels[house.ModelPath] = list;
                }
                list.Add(house.WorldMatrix);
                //house.DrawCollisionChamber(gizmos, simulation);
            }
        }
        // se actualiza con las instancias visibles
        foreach (var group in _houseGroups)
        {
            if (visibleModels.TryGetValue(group.Key, out var visibleMatrices))
                group.Value.SetVisibleInstances(visibleMatrices);
            else
                group.Value.SetVisibleInstances(new List<Matrix>()); // nada visible de ese modelo
        }
        int totalVisible = visibleModels.Values.Sum(l => l.Count);
        //Console.WriteLine($"Casas Visibles: {totalVisible} / {_houses.Count}");
    }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var group in _houseGroups.Values)
        {
            // El grupo sabe cómo dibujar todas sus instancias eficientemente
            group.Draw(view, projection);
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        foreach (var group in _houseGroups.Values)
        {
            group.DrawDepth(lightViewProjection);
        }
    }

    //Me da una posicion aleatoria sobre el terreno
    private Vector3 GetRandomPosition()
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = _random.NextSingle() * horizontalRange + minHorizontal;
        var z = _random.NextSingle() * horizontalRange + minHorizontal;
        return new Vector3(x, _terrain.GetHeight(x, z), z);
    }

    //Genero una nueva casa segun una posicion
    public House GetHouse(Vector3 position)
    {
        return new House(position, GetRandomHousePath());
    }

    // Genera el path dentro del directorio Models de un modelo de casa para colocar en el mapa  
    private string GetRandomHousePath()
    {
        var index =  _random.Next(0, HouseModelPaths.Length);
        return HouseModelPaths[index];
    }
    
    // Genera posiciones aleatorias para todas las casas dentro del terreno
    private List<Vector3> GetValidHousePositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceBetween = 45f; // GE 4500f
        var minDistanceToSpawn =  20f;
        var spawnPoint = new Vector3(0, _terrain.GetHeight(0, 0), 0);

        // inicializacion
        for (int i = 0; i < NumberOfHouseModels; i++)
            positions.Add(GetRandomPosition());

        // chequeo
        for (int i = 0; i < NumberOfHouseModels; i++)
        {
            bool valid = false;
            while (!valid)
            {
                valid = true;
                for (int j = 0; j < i; j++)  // solo compara contra las anteriores ya validadas
                {
                    if (Vector3.Distance(positions[i], positions[j]) < minDistanceBetween ||
                        Vector3.Distance(positions[i], spawnPoint) < minDistanceToSpawn)
                    {
                        positions[i] = GetRandomPosition();
                        valid = false;
                        break;
                    }
                }
            }
        }

        return positions;
    }

    public List<Vector3> getHouses()
    {
        return _houses.Select(house => house.Position).ToList();
    }
}


