using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models.Tanks;
using static TGC.MonoGame.TP.GameConfig;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

public class EnemiesManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private int _enemiesCount = Enemies.EnemiesCount;
    public List<TankEnemy> _enemies = new();
    private List<BodyHandle> _enemiesHandles = new();
    private List<Vector3> _enemiesPositionsCache = new List<Vector3>(GameConfig.Enemies.EnemiesCount);
    
    //sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();
    //fisicas
    private Simulation _simulation;

    private GraphicsDevice _graphicsDevice;
    public Dictionary<BodyHandle, TankEnemy> EnemiesByHandle { get; private set; } = new();

    public EnemiesManager(Terrain terrain, Simulation simulation, GraphicsDevice graphicsDevice)
    {
        _terrain = terrain;
        _simulation = simulation;
        _graphicsDevice = graphicsDevice;
    }

    public void Initialize()
    {

    }

    public void LoadContent(ContentManager content)
    {
        var tankModel = content.Load<Model>(ContentFolder3D + Tank.TankModelPath);
        var tankTexture = content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        var tracksTexture = content.Load<Texture2D>(ContentFolderTextures + Tank.TankTracksTexture);
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");

        for (int i = 0; i < _enemiesCount; i++)
        {
            // Inicializo los tanques y sus handles
            TankEnemy enemy = _random.Next(3) switch
            {
                0 => new TankEnemyScout(_graphicsDevice),
                1 => new TankEnemyMedium(_graphicsDevice),
                _ => new TankEnemyHeavy(_graphicsDevice)
            };
            enemy.Position = enemy.GetPosition(_terrain, _random);
            enemy.Load(tankModel, tankTexture, tracksTexture, effect, _simulation);
            _enemies.Add(enemy);
            _enemiesHandles.Add(enemy.TankHandler);
            EnemiesByHandle[enemy.TankHandler] = enemy;
        }
    }

    public List<Vector3> GetEnemiesPositions()
    {
        _enemiesPositionsCache.Clear();
        foreach(var enemy in _enemies)
            if(!enemy.IsDead) _enemiesPositionsCache.Add(enemy.Position);
        return _enemiesPositionsCache;
    }

    public void Update(GameTime gameTime, Vector3 position) {
        //Transitar de atras para adelante para poder borrar elementos sin romper los indices
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var tankEnemy = _enemies[i];

            if (tankEnemy.IsDead)
            {
                //Eliminar el cuerpo fisico
                _simulation.Bodies.Remove(tankEnemy.TankHandler);

                EnemiesByHandle.Remove(tankEnemy.TankHandler);

                //Remover el handle de la lista auxiliar para evitar leaks
                _enemiesHandles.Remove(tankEnemy.TankHandler);

                //Remover enemigo de la lista principal
                _enemies.RemoveAt(i);

                //Salteamos UpdateEnemy
                continue;
            }

            tankEnemy.UpdateEnemy(gameTime, _simulation, position.ToNumerics());

        }
     }

    public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition, BoundingFrustum CameraFrustum)
    {
        int totalVisible = 0;
        foreach(var tankEnemy in _enemies)
        {
            if(CameraFrustum.Intersects(tankEnemy._worldBoundingVolume)) {
                tankEnemy.Draw(view, projection, cameraPosition);
                totalVisible++;
            }
        }
        //Console.WriteLine($"Casas Visibles: {totalVisible} / {_enemiesCount}");
    }

    public void DrawDepth(Matrix lightViewProjection, BoundingFrustum CameraFrustum)
    {
        foreach(var tankEnemy in _enemies)
        {
            if(CameraFrustum.Intersects(tankEnemy._worldBoundingVolume))
                tankEnemy.DrawDepth(lightViewProjection);
        }
    }

    public void Reset(Simulation simulation)
    {
        // remover cuerpos físicos de la simulación
        foreach (var handle in _enemiesHandles)
            simulation.Bodies.Remove(handle);

        _enemies.Clear();
        _enemiesHandles.Clear();
        EnemiesByHandle.Clear();

        // recargar a los enemigos
        LoadContent(TGCGame.Instance.Content);
    }
}
