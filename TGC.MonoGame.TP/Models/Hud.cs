using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TGC.MonoGame.TP.Managers;
using TGC.MonoGame.TP.Models.Decorations;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     hud simple que muestra los controles del juego en pantalla.
///     Implementacion responsive y indicador de enfriamiento del canon.
/// </summary>
public class Hud
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    private Color _fuelColorCache;
    private Color _healthColorCache;
    private Color _cooldownColorCache;
    private Rectangle _fuelBarBgRect;
    private Rectangle _fuelBarFillRect;
    private Rectangle _healthBarBgRect;
    private Rectangle _healthBarFillRect;
    private Rectangle _cooldownBarBgRect;
    private Rectangle _cooldownBarFillRect;
    private Rectangle _minimapRect;
    private Rectangle _legendRect;

    // Parámetros de diseño responsive
    private readonly float _paddingPercentX = 0.02f;
    private readonly float _paddingPercentY = 0.02f;
    private readonly float _spacingPercent = 0.008f;

    // Elementos gráficos para la barra de progreso vectorial
    private Texture2D _whitePixel;
    private const int ProgressBarWidth = 150;
    private const int ProgressBarHeight = 12;

    // Variables de cálculo para los FPS
    private float _fps;
    private float _fpsAccumulator;
    private int _fpsFrameCount;

    private List<DamageNumber> _damageNumbers = new List<DamageNumber>();

    // --- VARIABLES DE CACHÉ REACTIVA (Evitan miles de allocations de strings por segundo) ---

    private string _cachedFuelText = "FUEL: 100 / 100";
    private int _lastDisplayedFuel = -1;

    private string _cachedCooldownText = "DISPARO: LISTO";
    private float _lastRemainingSeconds = -1f;

    private string _cachedEnemiesCount = "KILL COUNT: 0 / " + GameConfig.Enemies.KillsToWin.ToString();
    private int _lastDisplayedEnemies = -1;
    private int _playerHealth = 100;

    private string _cachedPlayerHealth = "Health : / ";
    private float _lastDisplayedHealth = -1.0f;

    // MINIMAP
    private const int MinimapSize = 200;
    public float WidthUnits { get; set; }
    public float HeightUnits { get; set; }
    private const int iconDesWidth = 180;
    private const int iconDesHeight = 50;
    private Texture2D _playerTexture;
    private Texture2D _enemyTexture;
    private Texture2D _fuelTexture;

    // Propiedades expuestas que actualiza el juego en cada frame
    public Vector3 TankPosition { get; set; }
    public float TankRotation { get; set; } 
    public List<Vector3> EnemyPositions { get; set; } = new();
    public List<Vector3> FuelPositions { get;  set; } = new();
    public float TankFuel { get; set; }
    public float CannonCurrentCooldown { get; set; }
    public float CannonMaxCooldown { get; set; } = 0.5f;

    public Hud(){ }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");

        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        _playerTexture = content.Load<Texture2D>("Textures/minimap_playerv4");
        _enemyTexture = content.Load<Texture2D>("Textures/minimap_enemyv2");
        _fuelTexture = content.Load<Texture2D>("Textures/minimap_fuel");
    }

    public void AddDamageNumber(Vector3 worldPos, float value)
    {
        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        var camera = TGCGame.Instance.Camera;
        _damageNumbers.Add(new DamageNumber(worldPos, value, viewport, camera.View, camera.Projection));
    }

    /// <summary>
    ///     Actualiza el contador de FPS. Llamar desde TGCGame.Update()
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fpsAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fpsFrameCount++;

        // Recalcular el promedio defps cada segundo
        if (_fpsAccumulator >= 1f)
        {
            _fps = _fpsFrameCount / _fpsAccumulator;
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;
        }

        // === damage numbers ===
        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            _damageNumbers[i].Update(dt);
            if (_damageNumbers[i].IsDead) _damageNumbers.RemoveAt(i);
        }

        // calcular la cantidad de kills
        var kills = TGCGame.Instance.EnemiesKilled;
        if (kills != _lastDisplayedEnemies)
        {
            _cachedEnemiesCount = "KILL COUNT: " + kills + " / " + GameConfig.Enemies.KillsToWin.ToString();
            _lastDisplayedEnemies = kills;
        }

        // calculo la vida del jugador
        _playerHealth = getPlayerHealth();
        if (_playerHealth != _lastDisplayedHealth)
        {
            _cachedPlayerHealth = $"HEALTH: {_playerHealth} / {TGCGame.Instance._tank.initialHealth}";
            _lastDisplayedHealth = _playerHealth;            
        }

    }

    public void Draw()
    {
        // Obtener dimensiones actuales del viewport para adaptar el hud a cualquier resolucion
        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        float screenWidth = viewport.Width;
        float screenHeight = viewport.Height;

        // Calcular margenes y saltos de linea proporcionales al tamano de la pantalla
        float padX = screenWidth * _paddingPercentX;
        float padY = screenHeight * _paddingPercentY;
        float spacing = screenHeight * _spacingPercent;

        if (TGCGame.Instance.GameStateManager.IsGodMode)
        {
            string godText = "MODO GOD ACTIVADO";
            Vector2 godTextSize = _font.MeasureString(godText);
            Vector2 godTextPos = new Vector2(screenWidth / 2f - godTextSize.X / 2f, 20f);

            _spriteBatch.Begin();

            // Sombra
            _spriteBatch.DrawString(_font, godText, godTextPos + Vector2.One, Color.Black);
            // Texto principal
            _spriteBatch.DrawString(_font, godText, godTextPos, Color.Gold);

            _spriteBatch.End();
        }

        // Esta linea es la que desactiva el Z-Buffer
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // panel para colocar los indicadores
        Vector2 rightPanelPosition = new Vector2(screenWidth - 250f - padX, padY);
        float currentY = rightPanelPosition.Y;
        Vector2 leftPanelPosition = new Vector2(padX, padY);
        float leftCurrentY = leftPanelPosition.Y;

        // === INSTRUCCIONES (esquina superior izquierda) ===
        _ = new Vector2(padX, padY);

        var fpsPosition = new Vector2(screenWidth - 100f - padX, padY);

        _ = new Vector2(fpsPosition.X - 150f, fpsPosition.Y + _font.LineSpacing + spacing);

        // === COMBUSTIBLE DEL TANQUE (debajo de coordenadas) ===
        int fuelInt = (int)TankFuel;
        if (fuelInt != _lastDisplayedFuel)
        {
            _cachedFuelText = $"FUEL: {fuelInt} / 100";
            _lastDisplayedFuel = fuelInt;
        }

        var fuelPosition = new Vector2(rightPanelPosition.X, currentY);
        currentY += _font.LineSpacing + spacing;

        // ✅ OPTIMIZACIÓN: Cacheo el color en un campo para no crear un nuevo Color cada frame
        _fuelColorCache = TankFuel > 30f ? Color.Lime : TankFuel > 10f ? Color.Yellow : Color.Red;

        _spriteBatch.DrawString(_font, _cachedFuelText, fuelPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedFuelText, fuelPosition, _fuelColorCache);

        float fuelPercent = TankFuel / 100f;
        Vector2 fuelBarPosition = new Vector2(rightPanelPosition.X, currentY);
        currentY += ProgressBarHeight + spacing * 2;
        DrawBar(fuelBarPosition, _fuelColorCache, fuelPercent);

        // === VIDA RESTANTE ===
        float healthPercent = getPlayerHealth() / TGCGame.Instance._tank.initialHealth;

        // ✅ OPTIMIZACIÓN: Cacheo el color de vida
        _healthColorCache = healthPercent > 0.5f ? Color.Lime : healthPercent > 0.25f ? Color.Yellow : Color.Red;

        Vector2 healthTextPosition = new Vector2(rightPanelPosition.X, currentY);
        currentY += _font.LineSpacing + spacing;
        _spriteBatch.DrawString(_font, _cachedPlayerHealth, healthTextPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedPlayerHealth, healthTextPosition, _healthColorCache);

        Vector2 healthBarPosition = new Vector2(rightPanelPosition.X, currentY);
        currentY += ProgressBarHeight + spacing * 2;
        DrawBar(healthBarPosition, _healthColorCache, healthPercent);

        // === MINIMAP ===
        var minimapPosition = leftPanelPosition;
        leftCurrentY += MinimapSize + spacing;

        // ✅ OPTIMIZACIÓN: Cacheo el rectángulo del minimapa
        _minimapRect = new Rectangle((int)minimapPosition.X, (int)minimapPosition.Y, MinimapSize, MinimapSize);
        _spriteBatch.Draw(_whitePixel, _minimapRect, Color.Black * 0.7f);

        // dibuja jugador en el espacio de minimapa
        Vector2 playerMarker = PositionWorldToMinimap(TankPosition, _minimapRect);
        float tankRotation = -TankRotation; // ángulo Y del tanque

        _spriteBatch.Draw(_playerTexture, playerMarker, null, Color.Lime, tankRotation,
            new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);

        // dibuja enemigos en el espacio de minimapa
        foreach (var enemyPos in EnemyPositions)
        {
            Vector2 enemyMarker = PositionWorldToMinimap(enemyPos, _minimapRect);
            if (_minimapRect.Contains((int)enemyMarker.X, (int)enemyMarker.Y))
            {
                _spriteBatch.Draw(_enemyTexture, enemyMarker, null, Color.Red, 0f,
                    new Vector2(_enemyTexture.Width / 2f, _enemyTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);
            }
        }
        //dibuja los barriles de fuel
        foreach (var barrelPos in FuelPositions)
        {
            Vector2 barrelMarker = PositionWorldToMinimap(barrelPos, _minimapRect);
            if (_minimapRect.Contains((int)barrelMarker.X, (int)barrelMarker.Y))
            {
                _spriteBatch.Draw(_fuelTexture, barrelMarker, null, Color.Yellow, 0f,
                    new Vector2(_fuelTexture.Width / 2f, _fuelTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);
            }
        }

        // === BARRA DESCRIPTIVA DE ICONOS ===
        // ✅ OPTIMIZACIÓN: Cacheo el rectángulo de la leyenda
        _legendRect = new Rectangle((int)leftPanelPosition.X, (int)leftCurrentY, iconDesWidth, iconDesHeight);
        _spriteBatch.Draw(_whitePixel, _legendRect, Color.Black * 0.7f);

        DrawIconAndText(_enemyTexture, "Enemigo", new Vector2(_legendRect.X + 12, _legendRect.Y + 15), Color.Red, _legendRect);
        DrawIconAndText(_fuelTexture, "Combustible", new Vector2(_legendRect.X + 12, _legendRect.Y + 40), Color.Yellow, _legendRect);

        leftCurrentY += _legendRect.X + spacing * 3;

        // === ENEMIGOS DERROTADOS ===
        var killsPosition = new Vector2(leftPanelPosition.X, leftCurrentY);
        leftCurrentY += _font.LineSpacing + spacing;
        _spriteBatch.DrawString(_font, _cachedEnemiesCount, killsPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedEnemiesCount, killsPosition, Color.White);

        // === COOLDOWN ===
        float remaining = MathHelper.Max(0f, CannonCurrentCooldown);
        if (MathF.Abs(remaining - _lastRemainingSeconds) > 0.05f || (remaining == 0f && _lastRemainingSeconds > 0f))
        {
            _cachedCooldownText = remaining > 0f ? $"DISPARO: {remaining:F1}s" : "DISPARO: LISTO";
            _lastRemainingSeconds = remaining;
        }

        var cooldownPosition = new Vector2(leftPanelPosition.X, leftCurrentY);
        leftCurrentY += _font.LineSpacing + spacing;
        var cooldownBarPos = new Vector2(leftPanelPosition.X, leftCurrentY);
        leftCurrentY += ProgressBarHeight + spacing * 2;

        // ✅ OPTIMIZACIÓN: Cacheo el color del cooldown
        _cooldownColorCache = remaining <= 0f ? Color.Lime : Color.Orange;

        _spriteBatch.DrawString(_font, _cachedCooldownText, cooldownPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedCooldownText, cooldownPosition, _cooldownColorCache);

        // --- BARRA DE PROGRESO VECTORIAL GRÁFICA (Ahorra un 100% de Garbage Collector en este render) ---
        float barPercent = CannonMaxCooldown > 0f ? (1f - (remaining / CannonMaxCooldown)) : 1f;
        int activeWidth = (int)(barPercent * ProgressBarWidth);
        DrawBar(cooldownBarPos, _cooldownColorCache, barPercent);

        // === damage numbers ===
        foreach (var dmgNum in _damageNumbers) dmgNum.Draw(_spriteBatch, _font);

        _spriteBatch.End();
    }

    private int getPlayerHealth() => (int)TGCGame.Instance._tank.HealthPoints;

    private void DrawBar(Vector2 position, Color color, float percent)
    {
        percent = MathHelper.Clamp(percent, 0f, 1f);

        int activeWidth = (int)(percent * ProgressBarWidth);

        var barBgRect = new Rectangle((int)position.X, (int)position.Y, ProgressBarWidth, ProgressBarHeight);
        var barFillRect = new Rectangle((int)position.X, (int)position.Y, activeWidth, ProgressBarHeight);

        _spriteBatch.Draw(_whitePixel, barBgRect,Color.Black * 0.4f);
        _spriteBatch.Draw(_whitePixel, barFillRect, color);
    }

    private void DrawIconAndText(Texture2D iconTexture, string text, Vector2 iconPosition, Color iconColor, Rectangle legendRect)
    {
        float iconScale = 0.4f;

        _spriteBatch.Draw(iconTexture, iconPosition, null, iconColor, 0f, 
            new Vector2(iconTexture.Width / 2f, iconTexture.Height / 2f), iconScale, SpriteEffects.None, 0f);

        Vector2 textPos = new Vector2(iconPosition.X + 18f, iconPosition.Y - _font.LineSpacing / 2f);
        Vector2 textSize = _font.MeasureString(text);
        textPos.X = MathHelper.Clamp(textPos.X, legendRect.Left, legendRect.Right - textSize.X);
        textPos.Y = MathHelper.Clamp(textPos.Y, legendRect.Top, legendRect.Bottom - textSize.Y);

        _spriteBatch.DrawString(_font, text, textPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, text, textPos, Color.White);   
    }

    /// <summary>
    /// Convierte una posición 3D del mundo a coordenadas 2D del minimapa.
    /// Usa GameConfig.Terrain para derivar los límites reales del mapa.
    /// </summary>
    private Vector2 PositionWorldToMinimap(Vector3 worldPos, Rectangle mapRect)
    {
        float nx = MathHelper.Clamp((worldPos.X + WidthUnits) / (WidthUnits * 2f), 0f, 1f);
        float nz = MathHelper.Clamp((worldPos.Z + HeightUnits) / (HeightUnits * 2f), 0f, 1f);   

        float x = mapRect.X + nx * mapRect.Width;
        float y = mapRect.Y + nz * mapRect.Height;

        // Margen para que el icono no se salga visualmente
        const float markerMargin = 8f;

        x = MathHelper.Clamp(
            x,
            mapRect.Left + markerMargin,
            mapRect.Right - markerMargin);

        y = MathHelper.Clamp(
            y,
            mapRect.Top + markerMargin,
            mapRect.Bottom - markerMargin);

        return new Vector2(x, y);
    }


    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
    }
}