using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP.Managers;

public class GameStateManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    public GameState CurrentState { get; private set; } = GameState.Menu;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _fontArial;
    private readonly SpriteFont _fontConsolas;
    private readonly Texture2D _whitePixel;
    private Texture2D _menuBackground;
    private float _introTimer = 0f;
    private float _menuInputLockTime = 0f;

    //SoundManager
    private readonly SoundManager _soundManager;
    private bool _menuMusicStarted = false;

    // 3D Menu Variables
    private Model _currentMenuTankModel;
    private Texture2D _menuTankTexture;
    private Texture2D _menuTracksTexture;
    private Texture2D _menuSandTexture;
    private Effect _menuTankEffect;
    private float _menuTankRotation = 0f;
    private float _menuTankRotationSpeed = 0.015f;

    // diorama
    private Model _menuTerrainModel;
    private Model _menuArbolMuerto1Model;
    private Model _menuBarrilModel;
    private Model _menuCactus1Model;
    private Model _menuCarretaModel;
    private Model _menuCasitaMedianaModel;
    private Model _menuPozoModel;
    private Matrix _terrainWorld;
    private Matrix _arbolMuerto1World;
    private Matrix _barrilWorld;
    private Matrix _cactus1World;
    private Matrix _carretaWorld;
    private Matrix _casitaMedianaWorld;
    private Matrix _pozoWorld;

    // Opciones de menu actualizadas para elegir el tipo de tanque
    private readonly string[] _menuOptions = {
        "Scout",
        "Medium",
        "Heavy",
        "Salir"
    };
    private int _selectedIndex = 0; //Preselecciona Iniciar en el menu
    private int _lastHoveredIndex = -1; //preseleccion del mouse

    private MouseState _lastMouseState;
    private GameState _previousState = GameState.Menu;

    private ContentManager _menuContent; //evita cache compartido

    //exponerlo para reproducir efectos 3d
    public SoundManager SoundManager => _soundManager;

    public bool IsGodMode => CurrentState == GameState.GodMode;
    public bool IsGameRunning => CurrentState == GameState.Playing || IsGodMode;

    //Constante compartida de separacion vertical para las opciones del menu
    private const float OptionSpacing = 20f;

    private string _cachedSpecsText = string.Empty;
    private int _specsCachedIndex = -1;

    // Idle animation variables
    private float _idleTime = 0f;
    private const float IdleAnimationSpeed = 2.5f; // Controls how fast the pulse is

    private Texture2D _winTexture;
    private Texture2D _loseTexture;

    private ShadowMapManager _menuShadowMapManager;

    public GameStateManager(GraphicsDevice graphicsDevice, ContentManager content, SoundManager soundManager)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _soundManager = soundManager;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        // Coincide con la ruta compilada en Content.mgcb
        _fontArial = content.Load<SpriteFont>("SpriteFonts/ArialFont");
        _fontConsolas = content.Load<SpriteFont>("SpriteFonts/ConsolasFont");
        _menuBackground = content.Load<Texture2D>("Textures/ConceptArt6");

        //Textura 1x1 para overlays
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        LoadMenu3DAssets();

        CurrentState = GameState.Intro;
        _introTimer = 0f;
    }

    private void LoadMenu3DAssets()
    {
        try
        {
            //De este modo el modelo del menu es distinto al del juego
            _menuContent = new ContentManager(_content.ServiceProvider, "Content");

            //_menuTankEffect = _content.Load<Effect>("Effects/BasicShaderTexture");
            _menuTankEffect = _menuContent.Load<Effect>("Effects/ShadowMap");
            _menuTankEffect.Parameters["LightColor"]?.SetValue(new Vector3(0.65f, 0.55f, 0.40f));
            _menuTankEffect.Parameters["AmbientColor"]?.SetValue(new Vector3(0.25f, 0.25f, 0.25f));
            _menuTankTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
            _menuSandTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + "sand_seamless");
            _winTexture = _content.Load<Texture2D>("Textures/game-over/win");
            _loseTexture = _content.Load<Texture2D>("Textures/game-over/lose");
            _menuTracksTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + GameConfig.Tank.TankTracksTexture);
            _currentMenuTankModel = _menuContent.Load<Model>(ContentFolder3D + GameConfig.Tank.TankModelPath);

            //assets del diorama
            _menuTerrainModel = _menuContent.Load<Model>("Models/menu3d/menu3d v1");
            _menuArbolMuerto1Model = _menuContent.Load<Model>("Models/decoraciones/arbol_muerto_1");
            _menuBarrilModel = _menuContent.Load<Model>("Models/decoraciones/barril");
            _menuCactus1Model = _menuContent.Load<Model>("Models/decoraciones/cactus_1");
            _menuCasitaMedianaModel = _menuContent.Load<Model>("Models/casas/casita_mediana");
            _menuPozoModel = _menuContent.Load<Model>("Models/decoraciones/pozo");
            _menuCarretaModel = _menuContent.Load<Model>("Models/decoraciones/carreta_1");

            Matrix fixRotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90f));

            _terrainWorld = Matrix.CreateScale(1) * fixRotation * 
                            Matrix.CreateRotationY(MathHelper.ToRadians(90f)) * 
                            Matrix.CreateTranslation(0f, 0f, 0f);
            _arbolMuerto1World = Matrix.CreateScale(1) * fixRotation *
                          Matrix.CreateRotationY(MathHelper.ToRadians(-35f)) *
                          Matrix.CreateTranslation(-3f, 0f, -16f);
            _barrilWorld = Matrix.CreateScale(1) * fixRotation *
                          Matrix.CreateRotationY(MathHelper.ToRadians(-35f)) *
                          Matrix.CreateTranslation(-9f, 0f, -5f);
            _cactus1World = Matrix.CreateScale(1) * fixRotation *
                            Matrix.CreateRotationY(MathHelper.ToRadians(-35f)) *
                            Matrix.CreateTranslation(3f, 0f, -3f);
            _carretaWorld = Matrix.CreateScale(1) * fixRotation *
                            Matrix.CreateTranslation(-3f, 0f, -4f);
            _casitaMedianaWorld = Matrix.CreateScale(1) * fixRotation *
                          Matrix.CreateRotationY(MathHelper.ToRadians(-35f)) *
                          Matrix.CreateTranslation(-10f, 0f, -8f);
            _pozoWorld = Matrix.CreateScale(1) * fixRotation * 
                         Matrix.CreateTranslation(5f, 0f, -2f);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading 3D menu assets: {ex.Message}");
        }

        _menuShadowMapManager = new ShadowMapManager(_graphicsDevice, 2048)
        {
            LightPosition = new Vector3(15f, 20f, 10f),
            LightTarget = Vector3.Zero
        };

        _menuShadowMapManager.FitStaticToScene(
            new Vector3(-15f, -2f, -20f),
            new Vector3(15f, 10f, 5f)
        );
    }

    public void HandleMenuState(KeyboardState kb, KeyboardState lastkb)
    {
        HandleMenuInput(kb, lastkb);
    }

    public void Update(GameTime gameTime, KeyboardState kb, KeyboardState lastKb)
    {
        HandleMusic();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (CurrentState == GameState.Intro)
        {
            _introTimer += dt;

            bool keyPressed = kb.GetPressedKeys().Length > 0 && lastKb.GetPressedKeys().Length == 0;

            MouseState currentMouse = Mouse.GetState();
            bool mouseClicked = currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            _lastMouseState = currentMouse;

            if (_introTimer >= 8f || keyPressed || mouseClicked)
            {
                CurrentState = GameState.Menu;
                _introTimer = 0f;
                _menuInputLockTime = 0.3f; //impide input bleed en GameState.Menu
            }
        }

        _menuTankRotation += _menuTankRotationSpeed;
        _idleTime += dt;

        //el menu maneja su propia logica, early return
        if (CurrentState == GameState.Menu)
        {
            //Bloquear input bleed por 0,3 seg
            if (_menuInputLockTime > 0f)
            {
                _menuInputLockTime -= dt;
                return;
            }

            HandleMenuState(kb, lastKb);
            return;
        }

        switch (CurrentState)
        {
            case GameState.Playing:
            case GameState.GodMode:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                {
                    _previousState = CurrentState;
                    CurrentState = GameState.Paused;
                }
                break;
            case GameState.Paused:
                // SoundManager.StopMusic();
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = _previousState;
                break;
            case GameState.GameOver:
            case GameState.Win: // por ahora, mismo comportamiento que cuando se pierde
                SoundManager.StopMusic();
                bool anyKeyPressed = kb.GetPressedKeys().Length > 0 && lastKb.GetPressedKeys().Length == 0;

                if (anyKeyPressed)
                {
                    CurrentState = GameState.Menu;
                    _selectedIndex = 0;
                    _menuMusicStarted = false;

                    TGCGame.Instance.IsMouseVisible = true;
                }
                break;
        }
    }

    private void HandleMenuInput(KeyboardState kb, KeyboardState lastKb)
    {
        // Teclado: flechas arriba/abajo
        if ((kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) && (lastKb.IsKeyUp(Keys.Down) && lastKb.IsKeyUp(Keys.S)))
            {
            TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");
            _selectedIndex = (_selectedIndex + 1) % _menuOptions.Length;
        }
            
        else if ((kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) && (lastKb.IsKeyUp(Keys.Up) && lastKb.IsKeyUp(Keys.W)))
        {
            TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");
            _selectedIndex = (_selectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
        }
            
        // Teclado: enter
        if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
            ApplySelection();

        // Mouse: hover y click
        MouseState currentMouse = Mouse.GetState();
        int hoveredIndex = GetOptionAtPosition(currentMouse.X, currentMouse.Y);
        if (hoveredIndex != -1)
        {
            if (hoveredIndex != _lastHoveredIndex)
                TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");

            _lastHoveredIndex = hoveredIndex;

            _selectedIndex = hoveredIndex; // Feedback de hover
            if (currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
                ApplySelection(); // Click selecciona esa opcion
        }
        _lastMouseState = currentMouse;
    }

    /// <summary>
    /// Determina si el cursor del mouse esta sobre alguna opcion del menu.
    /// Devuelve el indice de la opcion bajo el cursor, o -1 si no esta sobre ninguna.
    /// </summary>
    private int GetOptionAtPosition(int mouseX, int mouseY)
    {
        // 1. Obtener las dimensiones actuales de la ventana/pantalla
        var vp = _spriteBatch.GraphicsDevice.Viewport;

        // 2. Calcular el punto central exacto de la pantalla
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        // 3. Calcular la coordenada Y inicial para que el bloque completo de opciones quede centrado verticalmente.
        //    Se toma la mitad del alto total estimado del texto y se resta del centro.
        float pulse = (MathF.Sin(_idleTime * IdleAnimationSpeed) + 1f) / 2f;
        float scalePulse = 1.0f + pulse * 0.03f;

        // 5. Recorrer cada opcion del menu para verificar si el mouse esta dentro de su area visual
        for (int i = 0; i < _menuOptions.Length; i++)
        {
            bool isSelected = (i == _selectedIndex);
            float currentScale = isSelected ? scalePulse : 1f;

            Rectangle rect = CalculateOptionRectangle(i, center, currentScale);

            // Verificar si las coordenadas del mouse estan dentro de este rectangulo
            if (rect.Contains(mouseX, mouseY))
                return i; // ¡Encontrado! Devolver el indice de la opcion
        }

        // Si el bucle termina sin encontrar coincidencia, el mouse no esta sobre ninguna opcion
        return -1;
    }

    private void ApplySelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Iniciar Scout
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Scout;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 1: // Iniciar Medio
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Medium;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 2: // Iniciar Pesado
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Heavy;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 3: // Salir
                Environment.Exit(0);
                break;
        }
    }

    public void Draw(string extraInfo = "")
    {
        if (CurrentState == GameState.Playing) return; // En Playing no dibuja nada extra

        var vp = _graphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        if (CurrentState == GameState.Intro)
        {
            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //Dibujar la textura estirada a toda la pantalla
            _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);

            //Texto de "Presiona cualquier tecla o clic para continuar"
            if (_introTimer > 1.0f)
            {
                //Efecto de "pulso"
                float pulse = (MathF.Sin(_introTimer * 4f) + 1f) / 2f; // Oscila suavemente entre 0 y 1
                float alpha = MathHelper.Clamp((_introTimer - 1.0f) * 2f, 0f, 1f); // Fade-in de 0.5 segundos
                float scale = 1.3f + (pulse * 0.1f); // Escala entre 1.3x y 1.4x

                string hint = "Presiona cualquier tecla o clic para continuar";

                //Usar ConsolasFont
                Vector2 hintSize = _fontConsolas.MeasureString(hint) * scale;
                Vector2 hintPos = new Vector2(vp.Width / 2f - hintSize.X / 2f, vp.Height - 120f);

                //Fondo semitransparente detras del texto
                Rectangle bgRect = new Rectangle((int)hintPos.X - 30, (int)hintPos.Y - 15, (int)hintSize.X + 60, (int)hintSize.Y + 30);
                _spriteBatch.Draw(_whitePixel, bgRect, new Color(0, 0, 0, (int)(alpha * 200)));

                //Sombra gruesa y desplazada
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos + new Vector2(4, 4), new Color(0, 0, 0, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                //Texto principal pulsante
                Color textColor = Color.Lerp(Color.White, Color.Gold, pulse);
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos, new Color(textColor.R, textColor.G, textColor.B, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }

        if (CurrentState == GameState.Menu)
        {
            _graphicsDevice.Clear(Color.DarkSlateGray);

            Matrix view = Matrix.CreateLookAt(new Vector3(7f, 8f, 7f), new Vector3(0, 2, 0), Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, vp.AspectRatio, 0.1f, 100f);

            // --- DEPTH PASS ---
            var smm = _menuShadowMapManager;
            smm.BeginStaticShadowPass();
            var lvp = smm.LightViewProjection;

            _menuTankEffect.Parameters["normalOffsetScale"]?.SetValue(0.02f);
            DrawMenuDepth(_menuTerrainModel,       _terrainWorld,       lvp);
            DrawMenuDepth(_menuArbolMuerto1Model,  _arbolMuerto1World,  lvp);
            DrawMenuDepth(_menuBarrilModel,        _barrilWorld,        lvp);
            DrawMenuDepth(_menuCactus1Model,       _cactus1World,       lvp);
            DrawMenuDepth(_menuCasitaMedianaModel, _casitaMedianaWorld, lvp);
            DrawMenuDepth(_menuPozoModel,          _pozoWorld,          lvp);
            DrawMenuDepth(_menuCarretaModel,       _carretaWorld,       lvp);

            if (_selectedIndex < 3 && _currentMenuTankModel != null)
            {
                Matrix tankWorld = Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
                                Matrix.CreateRotationY(_menuTankRotation) *
                                Matrix.CreateTranslation(0f, 0.45f, 0f);
                DrawMenuDepth(_currentMenuTankModel, tankWorld, lvp);
            }

            // --- LIGHTING PASS ---
            smm.BeginLightingPass(_menuTankEffect);
            _graphicsDevice.Clear(Color.DarkSlateGray);

            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.Opaque;

            if (_menuTerrainModel != null)
            {
                DrawMenuEnvironmentModel(_menuTerrainModel,       _terrainWorld,       view, projection);
                DrawMenuEnvironmentModel(_menuArbolMuerto1Model,  _arbolMuerto1World,  view, projection);
                DrawMenuEnvironmentModel(_menuBarrilModel,        _barrilWorld,        view, projection);
                DrawMenuEnvironmentModel(_menuCactus1Model,       _cactus1World,       view, projection);
                DrawMenuEnvironmentModel(_menuCasitaMedianaModel, _casitaMedianaWorld, view, projection);
                DrawMenuEnvironmentModel(_menuPozoModel,          _pozoWorld,          view, projection);
                DrawMenuEnvironmentModel(_menuCarretaModel,       _carretaWorld,       view, projection);
            }

            if (_selectedIndex < 3 && _currentMenuTankModel != null)
            {
                Matrix tankWorld = Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
                                Matrix.CreateRotationY(_menuTankRotation) *
                                Matrix.CreateTranslation(0f, 0.45f, 0f);
                Draw3DTank(tankWorld, view, projection);
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (_selectedIndex < 3)
                DrawTankSpecs(vp);
            DrawMenu(new Vector2(vp.Width / 2f, vp.Height / 2f));
            _spriteBatch.End();
        }
        else if (CurrentState == GameState.Paused || CurrentState == GameState.GameOver || CurrentState == GameState.Win)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            if (CurrentState == GameState.Paused)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText("PAUSA\nPresiona P para continuar", center);
            }
            else if (CurrentState == GameState.GameOver || CurrentState == GameState.Win)
            {
                // 1. Dibujar la imagen de fondo a pantalla completa
                Texture2D bgTexture = (CurrentState == GameState.Win) ? _winTexture : _loseTexture;

                if (bgTexture != null)
                {
                    // Se dibuja estirada a todo el viewport
                    _spriteBatch.Draw(bgTexture, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);
                }
                else
                {
                    // Fallback por si acaso no cargo la textura
                    _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black);
                }

                // 2. Dibujar el texto pulsante abajo (Reutilizando la logica de Intro)
                float pulse = (MathF.Sin(_idleTime * 4f) + 1f) / 2f; // Oscila suavemente
                float scale = 1.0f + (pulse * 0.05f); // Escala entre 1.0x y 1.05x
                string hint = "Presiona cualquier tecla para continuar";

                Vector2 hintSize = _fontConsolas.MeasureString(hint) * scale;
                Vector2 hintPos = new Vector2(vp.Width / 2f - hintSize.X / 2f, vp.Height - 100f);

                // Fondo semitransparente detrás del texto para que resalte sobre la imagen
                Rectangle bgRect = new Rectangle((int)hintPos.X - 30, (int)hintPos.Y - 15, (int)hintSize.X + 60, (int)hintSize.Y + 30);
                _spriteBatch.Draw(_whitePixel, bgRect, new Color(0, 0, 0, 150));

                // Sombra del texto
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos + new Vector2(3, 3), new Color(0, 0, 0, 255), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // Texto principal con color pulsante (Blanco a Dorado)
                Color textColor = Color.Lerp(Color.White, Color.Gold, pulse);
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }
    }

    private void DrawMenuDepth(Model model, Matrix world, Matrix lightViewProjection)
    {
        if (model == null || _menuTankEffect == null) return;

        _menuTankEffect.CurrentTechnique = _menuTankEffect.Techniques["DepthPass"];
        _menuTankEffect.Parameters["World"]?.SetValue(world);
        _menuTankEffect.Parameters["LightViewProjection"]?.SetValue(lightViewProjection);
        _menuTankEffect.Parameters["IsDeformable"]?.SetValue(0);

        foreach (var mesh in model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
                part.Effect = _menuTankEffect;
            mesh.Draw();
        }
    }

    private void Draw3DTank(Matrix world, Matrix view, Matrix projection)
    {
        if (_currentMenuTankModel == null || _menuTankEffect == null || _menuTankTexture == null) return;

        var smm = _menuShadowMapManager;

        Vector3 menuTankColor = Vector3.One;
        if (_selectedIndex == 0)      menuTankColor = new Color(50, 205, 50).ToVector3();
        else if (_selectedIndex == 1) menuTankColor = new Color(255, 215, 0).ToVector3();
        else if (_selectedIndex == 2) menuTankColor = new Color(178, 34, 34).ToVector3();

        _menuTankEffect.Parameters["View"]?.SetValue(view);
        _menuTankEffect.Parameters["Projection"]?.SetValue(projection);
        _menuTankEffect.Parameters["normalOffsetScale"]?.SetValue(0.05f);
        _menuTankEffect.Parameters["Shininess"]?.SetValue(32f);
        _menuTankEffect.Parameters["IsDeformable"]?.SetValue(0);
        _menuTankEffect.Parameters["TrackOffset"]?.SetValue(0f);
        _menuTankEffect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
        _menuTankEffect.Parameters["LightViewProjection"]?.SetValue(smm.LightViewProjection);
        _menuTankEffect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
        _menuTankEffect.Parameters["shadowMapDynamic"]?.SetValue(smm.StaticShadowRenderTarget);
        _menuTankEffect.Parameters["shadowMapSize"]?.SetValue(new Vector2(2048, 2048));

        _menuTankEffect.CurrentTechnique = _menuTankEffect.Techniques["DrawShadowedHibrido"];

        foreach (var mesh in _currentMenuTankModel.Meshes)
        {
            Texture2D activeTexture = mesh.Name.Contains("Cadena") ? _menuTracksTexture : _menuTankTexture;

            Vector3 colorToApply = Vector3.One;
            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Anillo") ||
                mesh.Name.Contains("Proteccion_d") || mesh.Name.Contains("Proteccion_i") ||
                mesh.Name.Contains("Cuerpo") || mesh.Name.Contains("Cubre"))
                colorToApply = menuTankColor;

            _menuTankEffect.Parameters["World"]?.SetValue(world);
            _menuTankEffect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
            _menuTankEffect.Parameters["ModelTexture"]?.SetValue(activeTexture);
            _menuTankEffect.Parameters["DiffuseColor"]?.SetValue(colorToApply);

            foreach (var part in mesh.MeshParts)
                part.Effect = _menuTankEffect;

            mesh.Draw();
        }
    }

    private void DrawTankSpecs(Viewport vp)
    {
        if (_selectedIndex != _specsCachedIndex)
        {
            string className;
            float playerHealth, maxSpeed, motorForce, turnSpeed, attackDamage, cooldown;

            if (_selectedIndex == 0)
            {
                className = "SCOUT";
                playerHealth = GameConfig.TankClasses.Scout.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Scout.MaxSpeed;
                motorForce = GameConfig.TankClasses.Scout.MotorForce / 1000;
                turnSpeed = GameConfig.TankClasses.Scout.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Scout.AttackDamage;
                cooldown = GameConfig.TankClasses.Scout.Cooldown;
            }
            else if (_selectedIndex == 1)
            {
                className = "MEDIUM";
                playerHealth = GameConfig.TankClasses.Medium.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Medium.MaxSpeed;
                motorForce = GameConfig.TankClasses.Medium.MotorForce / 1000;
                turnSpeed = GameConfig.TankClasses.Medium.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Medium.AttackDamage;
                cooldown = GameConfig.TankClasses.Medium.Cooldown;
            }
            else
            {
                className = "HEAVY";
                playerHealth = GameConfig.TankClasses.Heavy.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Heavy.MaxSpeed;
                motorForce = GameConfig.TankClasses.Heavy.MotorForce / 1000;
                turnSpeed = GameConfig.TankClasses.Heavy.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Heavy.AttackDamage;
                cooldown = GameConfig.TankClasses.Heavy.Cooldown;
            }

            _cachedSpecsText = $"CLASE: {className}\n\n" +
                            $"HP Jugador:   {playerHealth}\n" +
                            $"Velocidad:    {maxSpeed}\n" +
                            $"Fuerza Motor: {motorForce}\n" +
                            $"Vel. Giro:    {turnSpeed}\n" +
                            $"Danio Ataque: {attackDamage:F1}\n" +
                            $"Cooldown:     {cooldown:F1}";

            _specsCachedIndex = _selectedIndex;
        }

        // ==========================================
        // POSICIONAMIENTO Y ANCHO FIJO
        // ==========================================
        // Usamos el mismo padX que el menu para que los bordes izquierdos coincidan exactamente
        float padX = vp.Width * 0.05f;
        float padY = vp.Height * 0.15f;
        float padding = 15f;

        // Medimos el texto actual y calculamos el ancho fijo para 8 numeros
        Vector2 textSize = _fontConsolas.MeasureString(_cachedSpecsText);
        //float widthFor8Chars = _fontConsolas.MeasureString("12345678").X;
        //float fixedWidth = Math.Max(textSize.X, widthFor8Chars);
        float fixedWidth = 400f;

        // El rectangulo empieza exactamente en padX (alineado con el panel de clases)
        Rectangle bgRect = new Rectangle(
            (int)padX,
            (int)(padY - padding),
            (int)(fixedWidth + padding * 2),
            (int)(textSize.Y + padding * 2)
        );

        // El texto se dibuja desplazado por el padding interno respecto al rectangulo
        Vector2 specsPos = new Vector2(padX + padding, padY);

        // ==========================================
        // DIBUJO DEL FONDO Y TEXTO
        // ==========================================
        // Fondo negro translucido
        _spriteBatch.Draw(_whitePixel, bgRect, new Color(0, 0, 0, 180));

        // Borde sutil para que combine con el menu (DrawRectOutline se agrego en el paso anterior)
        DrawRectOutline(bgRect, new Color(255, 200, 50, 100));

        // Texto con sombra
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos, Color.Gold);
    }

    private void DrawMenu(Vector2 center)
    {
        var vp = _graphicsDevice.Viewport;
        Vector2 menuStart = GetMenuStartPosition(vp);

        float pulse = (MathF.Sin(_idleTime * IdleAnimationSpeed) + 1f) / 2f;
        float arrowOffset = pulse * 4f;

        Color breathingColor = Color.Lerp(
            new Color(180, 140, 0),
            new Color(255, 223, 0),
            pulse
        );

        float scalePulse = 1.0f + pulse * 0.03f;

        // ==========================================
        // PANELES DE FONDO (Rectangulos translucidos)
        // ==========================================

        // --- Panel de Clases (incluye titulo + 3 opciones) ---
        string title = "ELIGE TU CLASE:";
        Vector2 titleSize = _fontArial.MeasureString(title);

        float panelPaddingX = 30f;
        float panelPaddingY = 20f;
        float titleToOptionsGap = 12f;

        // Altura total del panel de clases
        float optionsBlockHeight = 3 * (_fontArial.LineSpacing + OptionSpacing);
        float classPanelHeight = titleSize.Y + titleToOptionsGap + optionsBlockHeight + panelPaddingY * 2;

        // Ancho: el mas grande entre el titulo y la opcion mas ancha
        float maxOptionWidth = 0f;
        for (int i = 0; i < 3; i++)
        {
            float w = _fontArial.MeasureString(_menuOptions[i]).X;
            if (w > maxOptionWidth) maxOptionWidth = w;
        }
        float classPanelWidth = MathF.Max(titleSize.X, maxOptionWidth) + panelPaddingX * 2 + 50f; // +50 para las flechas

        // Posicion del panel de clases (alineado a la izquierda desde menuStart)
        float classPanelX = menuStart.X;
        float classPanelY = menuStart.Y;

        Rectangle classPanelRect = new Rectangle(
            (int)classPanelX, (int)classPanelY,
            (int)classPanelWidth, (int)classPanelHeight
        );

        // Dibujar fondo del panel de clases
        _spriteBatch.Draw(_whitePixel, classPanelRect, new Color(0, 0, 0, 160));
        DrawRectOutline(classPanelRect, new Color(255, 223, 0, 80));

        // --- Panel de Salir (separado, abajo) ---
        string exitOption = _menuOptions[3];
        Vector2 exitSize = _fontArial.MeasureString(exitOption);
        float exitPanelWidth = exitSize.X + panelPaddingX * 2 + 50f;
        float exitPanelHeight = _fontArial.LineSpacing + panelPaddingY * 2;

        float exitPanelX = menuStart.X;
        float exitPanelY = classPanelY + classPanelHeight + 15f; // 15px de separacion

        Rectangle exitPanelRect = new Rectangle(
            (int)exitPanelX, (int)exitPanelY,
            (int)exitPanelWidth, (int)exitPanelHeight
        );

        // Dibujar fondo del panel de salir
        _spriteBatch.Draw(_whitePixel, exitPanelRect, new Color(0, 0, 0, 160));
        DrawRectOutline(exitPanelRect, new Color(255, 223, 0, 80));

        // ==========================================
        // TiTULO "ELIGE TU CLASE:"
        // ==========================================
        Vector2 titlePos = new Vector2(
            classPanelX + panelPaddingX,
            classPanelY + panelPaddingY
        );

        // Sombra del titulo
        _spriteBatch.DrawString(_fontArial, title, titlePos + new Vector2(2, 2), Color.Black);
        // Titulo con color dorado fijo
        _spriteBatch.DrawString(_fontArial, title, titlePos, new Color(255, 200, 50));

        // ==========================================
        // OPCIONES DE CLASE (SCOUT, MEDIUM, HEAVY)
        // ==========================================
        float optionsStartY = titlePos.Y + titleSize.Y + titleToOptionsGap;

        for (int i = 0; i < 3; i++)
        {
            string option = _menuOptions[i];
            bool isSelected = (i == _selectedIndex);
            float currentScale = isSelected ? scalePulse : 1f;

            Vector2 originalSize = _fontArial.MeasureString(option);
            Vector2 scaledSize = originalSize * currentScale;

            // Alinear a la izquierda dentro del panel (con padding)
            Vector2 pos = new Vector2(
                classPanelX + panelPaddingX,
                optionsStartY + i * (_fontArial.LineSpacing + OptionSpacing)
            );

            if (isSelected)
            {
                // Sombra
                _spriteBatch.DrawString(_fontArial, option, pos + new Vector2(2, 2), Color.Black,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                // Texto principal con color oscilante
                _spriteBatch.DrawString(_fontArial, option, pos, breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                // Flechas dinamicas
                var arrowSize = _fontArial.MeasureString("> ");

                _spriteBatch.DrawString(_fontArial, "> ",
                    new Vector2(pos.X - arrowSize.X + arrowOffset, pos.Y), breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                _spriteBatch.DrawString(_fontArial, " <",
                    pos + new Vector2(scaledSize.X - arrowOffset, 0), breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.DrawString(_fontArial, option, pos + new Vector2(2, 2), Color.Black);
                _spriteBatch.DrawString(_fontArial, option, pos, Color.White);
            }
        }

        // ==========================================
        // OPCION SALIR (en su propio panel)
        // ==========================================
        {
            bool isSelected = (3 == _selectedIndex);
            float currentScale = isSelected ? scalePulse : 1f;

            Vector2 originalSize = _fontArial.MeasureString(exitOption);
            Vector2 scaledSize = originalSize * currentScale;

            // Alinear a la izquierda dentro del panel
            Vector2 pos = new Vector2(
                exitPanelX + panelPaddingX,
                exitPanelY + (exitPanelHeight - scaledSize.Y) / 2f
            );

            if (isSelected)
            {
                _spriteBatch.DrawString(_fontArial, exitOption, pos + new Vector2(2, 2), Color.Black,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                // Color rojizo para "Salir" cuando esta seleccionado
                Color exitColor = Color.Lerp(new Color(180, 40, 40), new Color(255, 80, 80), pulse);
                _spriteBatch.DrawString(_fontArial, exitOption, pos, exitColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                var arrowSize = _fontArial.MeasureString("> ");

                _spriteBatch.DrawString(_fontArial, "> ",
                    new Vector2(pos.X - arrowSize.X + arrowOffset, pos.Y), exitColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                _spriteBatch.DrawString(_fontArial, " <",
                    pos + new Vector2(scaledSize.X - arrowOffset, 0), exitColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.DrawString(_fontArial, exitOption, pos + new Vector2(2, 2), Color.Black);
                _spriteBatch.DrawString(_fontArial, exitOption, pos, new Color(200, 200, 200));
            }
        }
    }

    private Rectangle CalculateOptionRectangle(int index, Vector2 center, float scale = 1f)
    {
        var vp = _graphicsDevice.Viewport;
        Vector2 menuStart = GetMenuStartPosition(vp);

        // === Mismas constantes que en DrawMenu ===
        string title = "ELIGE TU CLASE:";
        Vector2 titleSize = _fontArial.MeasureString(title);
        float panelPaddingX = 30f;
        float panelPaddingY = 20f;
        float titleToOptionsGap = 12f;

        // Calcular dimensiones del panel de clases (igual que en DrawMenu)
        float optionsBlockHeight = 3 * (_fontArial.LineSpacing + OptionSpacing);
        float classPanelHeight = titleSize.Y + titleToOptionsGap + optionsBlockHeight + panelPaddingY * 2;

        float maxOptionWidth = 0f;
        for (int i = 0; i < 3; i++)
        {
            float w = _fontArial.MeasureString(_menuOptions[i]).X;
            if (w > maxOptionWidth) maxOptionWidth = w;
        }
        float classPanelWidth = MathF.Max(titleSize.X, maxOptionWidth) + panelPaddingX * 2 + 50f;

        float classPanelX = menuStart.X;
        float classPanelY = menuStart.Y;

        float optionsStartY = classPanelY + panelPaddingY + titleSize.Y + titleToOptionsGap;

        if (index < 3)
        {
            // Opciones de clase
            string option = _menuOptions[index];
            Vector2 originalSize = _fontArial.MeasureString(option);
            Vector2 scaledSize = originalSize * scale;

            Vector2 pos = new Vector2(
                classPanelX + panelPaddingX,
                optionsStartY + index * (_fontArial.LineSpacing + OptionSpacing)
            );

            // Agregar un poco de margen para que sea mas facil clickear
            int hitMargin = 10;
            return new Rectangle(
                (int)pos.X - hitMargin,
                (int)pos.Y - hitMargin,
                (int)classPanelWidth - (int)panelPaddingX * 2 + (int)hitMargin * 2, // Ancho del panel menos padding
                (int)scaledSize.Y + hitMargin * 2
            );
        }
        else
        {
            // Opcion Salir
            string exitOption = _menuOptions[3];
            Vector2 exitSize = _fontArial.MeasureString(exitOption);
            float exitPanelWidth = exitSize.X + panelPaddingX * 2 + 50f;
            float exitPanelHeight = _fontArial.LineSpacing + panelPaddingY * 2;

            float exitPanelX = menuStart.X;
            float exitPanelY = classPanelY + classPanelHeight + 15f;

            // Retornar el rectangulo completo del panel de salir para facilitar el click
            return new Rectangle(
                (int)exitPanelX,
                (int)exitPanelY,
                (int)exitPanelWidth,
                (int)exitPanelHeight
            );
        }
    }

    /// <summary>
    /// Calcula la posicion donde deben empezar los paneles del menu (debajo de los stats del tanque).
    /// </summary>
    private Vector2 GetMenuStartPosition(Viewport vp)
    {
        float padX = vp.Width * 0.05f;
        float padY = vp.Height * 0.15f;

        // Calcular la altura del panel de stats
        Vector2 textSize = _fontConsolas.MeasureString(_cachedSpecsText);
        float bgPadding = 15f;
        float specsPanelHeight = textSize.Y + bgPadding * 2;

        // Los menus empiezan debajo del panel de stats con un pequeño gap
        float menuStartY = padY + specsPanelHeight + 25f; // 25px de separacion

        return new Vector2(padX, menuStartY);
    }

    private void DrawCenteredText(string text, Vector2 center)
    {
        var size = _fontArial.MeasureString(text);
        var pos = center - size / 2;
        _spriteBatch.DrawString(_fontArial, text, pos + new Vector2(2), Color.Black);
        _spriteBatch.DrawString(_fontArial, text, pos, Color.White);
    }

    public void ForceState(GameState state)
    {
        if (CurrentState != state)
        {
            _previousState = CurrentState;
            CurrentState = state;
        }
    }

    // Maneja la reproduccion de musica segun el estado del juego
    private void HandleMusic()
    {
        if (CurrentState == GameState.Menu)
        {
            // Reproducir musica del menu solo si no esta sonando
            if (!_menuMusicStarted && MediaPlayer.State != MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/100-song18", true);
                _menuMusicStarted = true;
            }
        }
        else
        {
            // Detener musica del menu al salir de ese estado
            if (_menuMusicStarted && MediaPlayer.State == MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/101-Juhani Junkala [Retro Game Music Pack] Level 1", true);
                _menuMusicStarted = false;
            }
        }
    }

    /// <summary>
    /// Dibuja un modelo generico del menu usando la configuracion de luz y textura del tanque.
    /// </summary>
    private void DrawMenuEnvironmentModel(Model model, Matrix world, Matrix view, Matrix projection)
    {
        if (model == null || _menuTankEffect == null) return;

        var smm = _menuShadowMapManager;

        _menuTankEffect.Parameters["View"]?.SetValue(view);
        _menuTankEffect.Parameters["Projection"]?.SetValue(projection);
         float normalOffset = CalculateNormalOffsetScale(model);
        _menuTankEffect.Parameters["normalOffsetScale"]?.SetValue(normalOffset);
        _menuTankEffect.Parameters["Shininess"]?.SetValue(32f);
        _menuTankEffect.Parameters["IsDeformable"]?.SetValue(0);
        _menuTankEffect.Parameters["TrackOffset"]?.SetValue(0f);
        _menuTankEffect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
        _menuTankEffect.Parameters["LightViewProjection"]?.SetValue(smm.LightViewProjection);
        _menuTankEffect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
        _menuTankEffect.Parameters["shadowMapDynamic"]?.SetValue(smm.StaticShadowRenderTarget);
        _menuTankEffect.Parameters["shadowMapSize"]?.SetValue(new Vector2(2048, 2048));

        _menuTankEffect.CurrentTechnique = _menuTankEffect.Techniques["DrawShadowedHibrido"];

        foreach (var mesh in model.Meshes)
        {
            Texture2D activeTexture = mesh.Name.Contains("Terreno") ? _menuSandTexture : _menuTankTexture;

            _menuTankEffect.Parameters["World"]?.SetValue(world);
            _menuTankEffect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
            _menuTankEffect.Parameters["ModelTexture"]?.SetValue(activeTexture);
            _menuTankEffect.Parameters["DiffuseColor"]?.SetValue(Vector3.One);

            foreach (var part in mesh.MeshParts)
                part.Effect = _menuTankEffect;

            mesh.Draw();
        }
    }

    private float CalculateNormalOffsetScale(Model model)
    {
        var bbox = BoundingVolumesUtils.CreateBoundingBox(model);
        var dimensions = bbox.Max - bbox.Min;
        var objectSize = Math.Max(dimensions.X, Math.Max(dimensions.Y, dimensions.Z));
        return MathHelper.Clamp(objectSize * 0.02f, 0.03f, 0.6f);
    }

    private void DrawRectOutline(Rectangle rect, Color color)
    {
        // Grosor fijo de 2 pixeles
        int thickness = 2;

        // Top
        _spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        _spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        _spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        _spriteBatch.Draw(_whitePixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void ToggleGodMode()
    {
        if (!IsGameRunning) return;

        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.GodMode;
            if (TGCGame.Instance?._tank != null)
            { 
                TGCGame.Instance._tank.HealthPoints = 999;
            }
        }
        else if (CurrentState == GameState.GodMode)
        {
            CurrentState = GameState.Playing;
            if (TGCGame.Instance?._tank != null)
            {
                TGCGame.Instance._tank.HealthPoints = TGCGame.Instance._tank.initialHealth;
            }
        }
    }

    public void RestoreState()
    {
        CurrentState = _previousState;
    }

    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
        _menuShadowMapManager?.Dispose();
        _menuContent?.Unload(); 
    }
}