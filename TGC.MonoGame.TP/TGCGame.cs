﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Transactions;
using TGC.MonoGame.Samples.Physics.Bepu;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Terrains;
using TGC.MonoGame.TP.Models.Decorations;
using TGC.MonoGame.TP.Managers;
using TGC.MonoGame.TP.Models.Tanks;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    //Carpetas
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //-----------JUEGO
    //graficos
    private readonly GraphicsDeviceManager _graphics;
    //efectos
    private Effect _shadowMapEffect;
    //teclado
    private KeyboardState _lastKeyboardState;
    //mouse
    private MouseState _previousMouseState;
    //hud
    private Hud _hud;
    public Hud Hud => _hud;
    //gamestate
    private GameStateManager _gameStateManager;
    public GameStateManager GameStateManager => _gameStateManager;
    //-----------TANQUE
    public TankPlayer _tank;
    private TankFollowCamera _camera;
    public TankFollowCamera Camera => _camera;
    //-----------TERRENO
    private Terrain _terrain;
    private Wall _wall;
    public StaticHandle TerrainHandle => _terrainStaticHandle;
    public Skybox _skybox;
    //-----------Manager
    public HousesManager _housesManager;
    public StaticsManager _staticsManager;
    public DinamicsManager _dinamicsManager;
    public BarrelsManager _barrelsManager;
    public EnemiesManager _enemiesManager;
    private ShadowMapManager _shadowMapManager;
    public ShadowMapManager ShadowMapManager => _shadowMapManager;
    private SoundManager _soundManager;
    public SoundManager SoundManager => _gameStateManager.SoundManager;
    public SimpleCollisionTracker CollisionTracker { get; private set; } = new SimpleCollisionTracker();
    //-----------FISICAS
    private Simulation _simulation;
    private BufferPool _bufferPool;
    private BodyHandle _tankHandle;
    private StaticHandle _terrainStaticHandle;
    public static TGCGame Instance { get; private set; } //Esto es para que lo use NarrowPhaseCallbacks
    //gizmos
    private Gizmo _gizmos = new();
    private CannonballManager _cannonballManager;
    public CannonballManager CannonballManager => _cannonballManager;

    // Variable para guardar la eleccion del jugador desde el menu
    public static GameConfig.TankClass SelectedPlayerTank;
    // Variable para contabilizar las kills del jugador
    public int EnemiesKilled = 0;

    public Gizmo Gizmos => _gizmos;

    // ------Optimizacion
    private BoundingFrustum _cameraFrustum; // frustum de la camara para hacer frustum culling
    public BoundingFrustum CameraFrustum => _cameraFrustum;

    public TGCGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Instance = this;
        Window.Title = "Tanquesitos";
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
        _graphics.HardwareModeSwitch = false;
        Content.RootDirectory = "Content";
        IsMouseVisible = true; //Oculto el mouse porque da dolor de cabeza
    }

    protected override void Initialize()
    {
        // Se activa el Backface Culling en sentido anti-horario (se renderizan las caras frontales de los triangulos)
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        InitializePhysics();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        //Sonido
        _soundManager = new SoundManager();
        _soundManager.LoadContent(Content);

        //GameStateManager
        _gameStateManager = new GameStateManager(GraphicsDevice, Content, _soundManager);

        //RECURSOS
        //shaders
        _shadowMapEffect = Content.Load<Effect>(ContentFolderEffects + "ShadowMap");
        //La luz es la misma para todos los objetos, asi que la seteamos desde el principio en el efecto de sombras y lo mismo para la luz ambiental
        _shadowMapEffect.Parameters["LightColor"]?.SetValue(new Vector3(0.65f, 0.55f, 0.40f));
        _shadowMapEffect.Parameters["AmbientColor"]?.SetValue(new Vector3(0.25f, 0.25f, 0.25f));

        //texturas
        var terrainTexture = Content.Load<Texture2D>("Models/heightmaps/heightmap_512x512");
        var groundTexture = Content.Load<Texture2D>(ContentFolderTextures + "sand_1024_seamless");
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        var tracksTexture = Content.Load<Texture2D>(ContentFolderTextures + GameConfig.Tank.TankTracksTexture);

        //CannonballManager
        _cannonballManager = new CannonballManager(_simulation);
        _cannonballManager.LoadContent(Content, ContentFolder3D + "cannonball/cannonball", ContentFolderEffects + "ShadowMap");

        //AUXILIARES
        Vector3 spawnPos = new Vector3(0, 0, 0);

        //TERRENO
        //Creo un terreno (suelo)
        _terrain = new Terrain(GraphicsDevice);
        //Le paso la textura y el efecto
        _terrain.LoadContent(terrainTexture, groundTexture, _shadowMapEffect);
        //fisicas
        _terrainStaticHandle = _terrain.CreatePhysicsTerrain(_simulation);
        //paredes invisibles
        _wall = new Wall(_simulation);
        _wall.LoadContent(_terrain);

        _shadowMapManager = new ShadowMapManager(GraphicsDevice, 8192)
        {
            LightPosition = new Vector3(0f, 350f, 100f),
            LightTarget = Vector3.Zero
        };

        float halfSize = _terrain.WidthUnits;
        float maxHeight = GameConfig.Terrain.MaxHeightMeters;
        _shadowMapManager.FitStaticToScene(
            new Vector3(-halfSize, 0f, -halfSize),
            new Vector3(halfSize, maxHeight, halfSize)
        );

        //ASSETS DECORATIVOS
        //casas
        _housesManager = new HousesManager(_terrain, GraphicsDevice);
        _housesManager.Initialize();
        _housesManager.LoadContent(Content, _simulation);
        //estaticos
        _staticsManager = new StaticsManager(_terrain, _housesManager.getHouses(), GraphicsDevice);
        _staticsManager.Initialize();
        _staticsManager.LoadContent(Content, _simulation);
        //dinamicos
        _dinamicsManager = new DinamicsManager(_terrain, _staticsManager.GetDecorations(), _housesManager.getHouses());
        _dinamicsManager.Initialize();
        _dinamicsManager.LoadContent(Content, _simulation);
        
        //ENEMIGOS
        _enemiesManager = new EnemiesManager(_terrain, _simulation, GraphicsDevice);
        _enemiesManager.LoadContent(Content);

        //BARRELS
        _barrelsManager = new BarrelsManager(_terrain, _staticsManager.GetDecorations(), _housesManager.getHouses());
        _barrelsManager.Initialize();
        _barrelsManager.LoadContent(Content, _simulation);

        //SKYBOX
        _skybox = new Skybox(GraphicsDevice);
        _skybox.LoadContent(Content);

        //TANQUE
        var kb = Keyboard.GetState();
        _gameStateManager.HandleMenuState(kb, _lastKeyboardState);
        _lastKeyboardState = kb;
        // Crear el tanque usando la eleccion del jugador
        _tank = new TankPlayer(GraphicsDevice, SelectedPlayerTank);
        var tankModel = Content.Load<Model>(ContentFolder3D + GameConfig.Tank.TankModelPath);
        //Determino una posicion para el tanque
        float terrainY = _terrain.GetHeight(spawnPos.X, spawnPos.Z);//Se spawnea unos metros por encima del terreno
        _tank.Position = new Vector3(spawnPos.X, terrainY + GameConfig.Tank.SpawnZMargin, spawnPos.Z);
        //Cargo el tanque
        _tank.Load(tankModel, tankTexture, tracksTexture, _shadowMapEffect, _simulation);
        //fisicas
        _tankHandle = _tank.TankHandler;

        //HUD
        _hud = new Hud();
        _hud.LoadContent(Content, GraphicsDevice);
        _hud.WidthUnits = _terrain.WidthUnits;
        _hud.HeightUnits = _terrain.HeightUnits;
        _hud.TankPosition = _tank.Position;
        _hud.TankRotation = _tank.CannonRotation;
        _hud.EnemyPositions = _enemiesManager.GetEnemiesPositions();
        _hud.FuelPositions = _barrelsManager.GetBarrelsPositions();

        //CAMARA
        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);
        _camera.Terrain = _terrain;

        _cameraFrustum = new BoundingFrustum(_camera.View * _camera.Projection);

        //GIZMOS
        _gizmos.LoadContent(GraphicsDevice, Content);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        _gameStateManager.Update(gameTime, kb, _lastKeyboardState);

        if (kb.IsKeyDown(Keys.G) && !_lastKeyboardState.IsKeyDown(Keys.G))
        {
            _gameStateManager.ToggleGodMode();
        }

        if (kb.IsKeyDown(Keys.O) && !_lastKeyboardState.IsKeyDown(Keys.O))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

        _lastKeyboardState = kb;

        //El update del juego ocurre unicamente en estado Playing o GodMode, sino se sale temprano
        if (!_gameStateManager.IsGameRunning)
        {
            _skybox.Update(gameTime);
            base.Update(gameTime);
            return;
        }

        if (kb.IsKeyDown(Keys.Escape)) Exit();

        CollisionTracker.BeginFrame();

        _simulation.Timestep(1 / 60f);

        _tank.Update(gameTime, kb, _simulation);

        //verificar si pueden recogerse los barriles
        foreach (var barrel in _barrelsManager._fuelBarrels)
        {
            if (!barrel.IsCollected) barrel.TryCollect(_tank, _simulation);
        }

        _camera.Update(gameTime, _tank.Position, _tank.TurretRotationWorld); //A la camara ahora le paso la posicion de la torreta en vez de la base
        _cameraFrustum.Matrix = _camera.View * _camera.Projection;

        _enemiesManager.Update(gameTime, _tank.Position);

        _housesManager.Update(_cameraFrustum, _gizmos, _simulation);
        _staticsManager.Update(_cameraFrustum);
        _dinamicsManager.Update(_simulation);
        _barrelsManager.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        // cada frame el tiempo restante de cooldown baja
        _cannonballManager.UpdateCooldown((float)gameTime.ElapsedGameTime.TotalSeconds);
        MouseState currentMouseState = Mouse.GetState();

        if (currentMouseState.LeftButton == ButtonState.Pressed 
            && (_previousMouseState.LeftButton == ButtonState.Released || _gameStateManager.IsGodMode )
            && _cannonballManager.CanFire)
        {
            Vector3 direction = _tank.CannonForward;
            direction.Normalize();

            // Posición desde donde sale la bala
            Vector3 spawnPosition = _tank.CannonMuzzlePosition + 
                (direction * GameConfig.Tank.CannonSpawnOffsetForward) +
                (Vector3.Up * GameConfig.Tank.CannonSpawnOffsetUp);

            float playerPitch = SelectedPlayerTank switch
            {
                GameConfig.TankClass.Scout => GameConfig.TankClasses.Scout.CannonPitch,
                GameConfig.TankClass.Heavy => GameConfig.TankClasses.Heavy.CannonPitch,
                                         _ => GameConfig.TankClasses.Medium.CannonPitch
            };

            _cannonballManager.Fire(spawnPosition, direction, _tank.AttackDamage, _gameStateManager.SoundManager, _camera.ListenerPosition, _camera.ListenerForward, true, playerPitch);
        }
        _previousMouseState = currentMouseState;

        _cannonballManager.Update(gameTime);
        
        _gizmos.UpdateViewProjection(_camera.View, _camera.Projection);

        _hud.TankFuel = _tank.CurrentFuel;
        if (_tank.CurrentFuel <= 30 && !_tank.IsDead)
            _gameStateManager.SoundManager.PlaySoundWithCooldown("bajo_combustible_2", 1000);

        _hud.TankPosition = _tank.Position;
        _hud.CannonCurrentCooldown = _cannonballManager.CurrentCooldown;
        _hud.CannonMaxCooldown = _cannonballManager.ShootCooldown;
        _hud.TankPosition = _tank.Position;
        _hud.TankRotation = _tank.RotationY;
        _hud.EnemyPositions = _enemiesManager.GetEnemiesPositions();
        _hud.FuelPositions = _barrelsManager.GetBarrelsPositions();
        _hud.Update(gameTime);

        if (_tank.IsDead) _gameStateManager.ForceState(GameState.GameOver);
        if (EnemiesKilled >= GameConfig.Enemies.KillsToWin) _gameStateManager.ForceState(GameState.Win);

        base.Update(gameTime);
    }

    public void ResetGame()
    {
        _cannonballManager.Clear();
        EnemiesKilled = 0;

        float playerCooldown = SelectedPlayerTank switch
        { GameConfig.TankClass.Scout => GameConfig.TankClasses.Scout.Cooldown,
            GameConfig.TankClass.Heavy => GameConfig.TankClasses.Heavy.Cooldown,
            _ => GameConfig.TankClasses.Medium.Cooldown,
        };
        _cannonballManager.ShootCooldown = playerCooldown;

        var tankModel = Content.Load<Model>(ContentFolder3D + GameConfig.Tank.TankModelPath);
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        var tracksTexture = Content.Load<Texture2D>(ContentFolderTextures + GameConfig.Tank.TankTracksTexture);

        Vector3 spawnPos = Vector3.Zero;
        float terrainY = _terrain.GetHeight(spawnPos.X, spawnPos.Z);

        _simulation.Bodies.Remove(_tankHandle);
        _tank = new TankPlayer(GraphicsDevice, SelectedPlayerTank);
        _tank.Position = new Vector3(spawnPos.X, terrainY + GameConfig.Tank.SpawnZMargin, spawnPos.Z);
        _tank.Load(tankModel, tankTexture, tracksTexture, _shadowMapEffect, _simulation);
        _tankHandle = _tank.TankHandler;

        _enemiesManager.Reset(_simulation);
        _dinamicsManager.ResetDynamics(_simulation);
        _barrelsManager.Reset(_simulation);

        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);
        _cameraFrustum = new BoundingFrustum(_camera.View * _camera.Projection);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logica de renderizado del juego.
        _ = (float)gameTime.TotalGameTime.TotalSeconds;
        //GraphicsDevice.Clear(Color.CornflowerBlue);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1f, 0);

        if (_gameStateManager.IsGameRunning || _gameStateManager.CurrentState == GameState.Paused)
        {
            var smm = _shadowMapManager;
            var lvp = smm.LightViewProjection; // una sola matriz para todo

            //pasadas de sombras
            if (smm.RebajarSombrasEstaticas)
            {
                smm.BeginStaticShadowPass();
                _terrain.DrawDepth(lvp);
                _housesManager.DrawDepth(lvp);
                _staticsManager.DrawDepth(lvp);
                smm.RebajarSombrasEstaticas = false;
            }

            var cameraCorners = GetCameraFrustumCorners();
            smm.FitDynamicToCamera(cameraCorners);
            smm.BeginDynamicShadowPass();
            _tank.DrawDepth(lvp);
            _enemiesManager.DrawDepth(lvp, CameraFrustum);
            _cannonballManager.DrawDepth(lvp);
            _dinamicsManager.DrawDepth(lvp, CameraFrustum);
            _barrelsManager.DrawDepth(lvp, CameraFrustum);
            
            //preparar lighting pass
            smm.BeginLightingPass(_shadowMapEffect);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //dibujar skybox
            _skybox.Draw(_camera.View, _camera.Projection, _camera.ListenerPosition);

            //dibujar el resto de la escena, encima del skybox
            _terrain.Draw(_camera.View, _camera.Projection, _camera.ListenerPosition);
            _tank.Draw(_camera.View, _camera.Projection, _camera.ListenerPosition);
            _cannonballManager.Draw(_camera.View, _camera.Projection);
            _housesManager.Draw(_camera.View, _camera.Projection);
            _staticsManager.Draw(_camera.View, _camera.Projection);
            _dinamicsManager.Draw(_camera.View, _camera.Projection, CameraFrustum);
            _barrelsManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation, CameraFrustum);
            _enemiesManager.Draw(_camera.View, _camera.Projection, _camera.ListenerPosition, CameraFrustum);

            _hud.Draw();
            _gizmos.Draw();
        }

        //El manager dibuja encima (Menu, Pausa, GameOver)
        string reason = _tank.IsDead ? (_tank.CurrentFuel <= 0f ? "Sin combustible" : "Tanque destruido") : "";
        _gameStateManager.Draw(reason);
    }

    public void InitializePhysics()
    {
        _bufferPool = new BufferPool();
        _simulation =
            Simulation.Create(_bufferPool,
                new NarrowPhaseCallbacks(),
                new PoseIntegratorCallbacks(new System.Numerics.Vector3(0, -9.8f, 0)),
                new SolveDescription(8, 1));
    }

    private Vector3[] GetCameraFrustumCorners()
    {
        var viewport = GraphicsDevice.Viewport;
        var nearCorners = new Vector3[4];
        var farCorners = new Vector3[4];

        var projection = _camera.Projection;
        var view = _camera.View;

        // Near plane corners
        nearCorners[0] = new Vector3(-1, -1, 0);
        nearCorners[1] = new Vector3(1, -1, 0);
        nearCorners[2] = new Vector3(1, 1, 0);
        nearCorners[3] = new Vector3(-1, 1, 0);

        // Far plane corners
        farCorners[0] = new Vector3(-1, -1, 1);
        farCorners[1] = new Vector3(1, -1, 1);
        farCorners[2] = new Vector3(1, 1, 1);
        farCorners[3] = new Vector3(-1, 1, 1);

        // Transformar a world space
        var invViewProj = Matrix.Invert(view * projection);
        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = Vector3.Transform(nearCorners[i], invViewProj);
            farCorners[i] = Vector3.Transform(farCorners[i], invViewProj);
        }

        return new[] { nearCorners[0], nearCorners[1], nearCorners[2], nearCorners[3],
                   farCorners[0], farCorners[1], farCorners[2], farCorners[3] };
    }

    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();
        _shadowMapManager?.Dispose();
        _simulation?.Dispose();
        _simulation = null;
        _bufferPool.Clear();
        _terrain?.Dispose();
        _hud?.Dispose();
        _gameStateManager?.Dispose();
        base.UnloadContent();
    }
}