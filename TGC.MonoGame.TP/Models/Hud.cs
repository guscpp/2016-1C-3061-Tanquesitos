using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TGC.MonoGame.TP.Managers;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models;

/// <summary>
/// HUD tactico, responsive y 100% optimizado (Zero-Allocation en Draw).
/// </summary>
public class Hud
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _fontLabels;   // Fuente pequeña para etiquetas (Arial)
    private SpriteFont _fontNumbers;  // Fuente grande y tech para números (Consolas)

    private Texture2D _whitePixel;
    private List<DamageNumber> _damageNumbers = new List<DamageNumber>();

    // --- TEXTURAS MINIMAPA ---
    private Texture2D _playerTexture;
    private Texture2D _enemyTexture;
    private Texture2D _fuelTexture;

    // --- LAYOUT RESPONSIVE (Pre-allocados para evitar GC) ---
    private Rectangle _rightPanelRect;
    private Rectangle _leftPanelRect;
    private Rectangle _minimapBgRect;
    private Rectangle _minimapRect;
    private Rectangle _tempRect; // Helper para dibujar barras, grid y leyendas sin allocar
    private Rectangle[] _minimapCorners = new Rectangle[8]; // 4 esquinas * 2 lineas

    // --- VARIABLES DE DISENO RESPONSIVE ---
    private int _segmentWidth;
    private int _segmentGap;
    private int _gridSpacing;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private readonly float _paddingPercentX = 0.02f;
    private readonly float _paddingPercentY = 0.02f;

    // --- COLORES EN CACHE (Cero allocations de structs en el Draw) ---
    private readonly Color _colorPanelBg = new Color(0, 0, 0, 200);
    private readonly Color _colorMinimapBg = new Color(0, 15, 0, 160); // Tinte verdoso radar
    private readonly Color _colorCorner = new Color(150, 255, 150, 220);
    private readonly Color _colorLabel = new Color(160, 160, 160, 255);
    private readonly Color _colorBarOuter = new Color(10, 10, 10, 220);
    private readonly Color _colorBarInner = new Color(30, 30, 30, 180);
    private readonly Color _colorSegmentEmpty = new Color(15, 15, 15, 150);
    private readonly Color _colorGrid = new Color(100, 255, 100, 40);

    // --- CACHE REACTIVA (Solo actualiza si el valor cambia) ---
    private string _cachedFuelValue = "100";
    private int _lastDisplayedFuel = -1;
    private string _cachedHealthValue = "30";
    private float _lastDisplayedHealth = -1.0f;
    private string _cachedKillsValue = "0 / 10";
    private int _lastDisplayedEnemies = -1;
    private string _cachedCooldownValue = "R E A D Y";
    private float _lastRemainingSeconds = -1f;

    // --- ETIQUETAS ESTÁTICAS (Letter-spacing manual) ---
    private const string LABEL_FUEL = "F U E L";
    private const string LABEL_HEALTH = "H E A L T H";
    private const string LABEL_KILLS = "K I L L S";
    private const string LABEL_COOLDOWN = "C O O L D O W N";

    // --- PROPIEDADES EXPOSTAS AL JUEGO ---
    public float WidthUnits { get; set; }
    public float HeightUnits { get; set; }
    public Vector3 TankPosition { get; set; }
    public float TankRotation { get; set; }
    public List<Vector3> EnemyPositions { get; set; } = new();
    public List<Vector3> FuelPositions { get; set; } = new();
    public float TankFuel { get; set; }
    public float CannonCurrentCooldown { get; set; }
    public float CannonMaxCooldown { get; set; } = 0.5f;

    public Hud() { }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _fontLabels = content.Load<SpriteFont>("SpriteFonts/ArialFont");
        _fontNumbers = content.Load<SpriteFont>("SpriteFonts/ConsolasFont");

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

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            _damageNumbers[i].Update(dt);
            if (_damageNumbers[i].IsDead) _damageNumbers.RemoveAt(i);
        }

        // === CACHE REACTIVO ===
        int fuelInt = (int)TankFuel;
        if (fuelInt != _lastDisplayedFuel)
        {
            _cachedFuelValue = fuelInt.ToString();
            _lastDisplayedFuel = fuelInt;
        }

        int healthInt = getPlayerHealth();
        if (healthInt != _lastDisplayedHealth)
        {
            _cachedHealthValue = healthInt.ToString();
            _lastDisplayedHealth = healthInt;
        }

        var kills = TGCGame.Instance.EnemiesKilled;
        if (kills != _lastDisplayedEnemies)
        {
            _cachedKillsValue = $"{kills} / {GameConfig.Enemies.KillsToWin}";
            _lastDisplayedEnemies = kills;
        }

        float remaining = MathHelper.Max(0f, CannonCurrentCooldown);
        if (MathF.Abs(remaining - _lastRemainingSeconds) > 0.05f || (remaining == 0f && _lastRemainingSeconds > 0f))
        {
            _cachedCooldownValue = remaining > 0f ? $"{remaining:F1}s" : "R E A D Y";
            _lastRemainingSeconds = remaining;
        }
    }

    public void Draw()
    {
        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        int screenWidth = viewport.Width;
        int screenHeight = viewport.Height;

        UpdateLayout(screenWidth, screenHeight);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // === GOD MODE TEXT ===
        if (TGCGame.Instance.GameStateManager.IsGodMode)
        {
            string godText = "MODO GOD ACTIVADO";
            Vector2 godTextSize = _fontNumbers.MeasureString(godText);
            Vector2 godTextPos = new Vector2(screenWidth / 2f - godTextSize.X / 2f, 20f);
            _spriteBatch.DrawString(_fontNumbers, godText, godTextPos + Vector2.One, Color.Black);
            _spriteBatch.DrawString(_fontNumbers, godText, godTextPos, Color.Gold);
        }

        // === FONDOS DE PANELES ===
        _spriteBatch.Draw(_whitePixel, _rightPanelRect, _colorPanelBg);
        _spriteBatch.Draw(_whitePixel, _leftPanelRect, _colorPanelBg);
        _spriteBatch.Draw(_whitePixel, _minimapBgRect, _colorMinimapBg);

        // === MINIMAP GRID (Retícula Radar) ===
        DrawMinimapGrid();

        // === MINIMAP CORNERS ===
        for (int i = 0; i < 8; i++)
        {
            _spriteBatch.Draw(_whitePixel, _minimapCorners[i], _colorCorner);
        }

        // === PANELES DE STATS ===
        DrawRightPanel();
        DrawLeftPanel();

        // === MINIMAP ICONS ===
        DrawMinimapIcons();
        DrawLegend();

        // === DAMAGE NUMBERS ===
        foreach (var dmgNum in _damageNumbers) dmgNum.Draw(_spriteBatch, _fontLabels);

        _spriteBatch.End();
    }

    #region Layout & Helpers (Zero-Allocation)

    private void UpdateLayout(int screenWidth, int screenHeight)
    {
        if (_lastScreenWidth == screenWidth && _lastScreenHeight == screenHeight) return;
        _lastScreenWidth = screenWidth;
        _lastScreenHeight = screenHeight;

        float padX = screenWidth * _paddingPercentX;
        float padY = screenHeight * _paddingPercentY;

        int panelWidth = (int)(screenWidth * 0.18f);
        int rightPanelHeight = (int)(screenHeight * 0.20f);
        int leftPanelHeight = (int)(screenHeight * 0.18f);

        _rightPanelRect.X = (int)(screenWidth - panelWidth - padX);
        _rightPanelRect.Y = (int)padY;
        _rightPanelRect.Width = panelWidth;
        _rightPanelRect.Height = rightPanelHeight;

        _leftPanelRect.X = (int)padX;
        _leftPanelRect.Y = (int)padY;
        _leftPanelRect.Width = panelWidth;
        _leftPanelRect.Height = leftPanelHeight;

        int minimapSize = (int)(Math.Min(screenWidth, screenHeight) * 0.22f);
        int minimapX = (int)padX;
        int minimapY = _leftPanelRect.Bottom + (int)(screenHeight * 0.02f);

        _minimapBgRect.X = minimapX; _minimapBgRect.Y = minimapY;
        _minimapBgRect.Width = minimapSize; _minimapBgRect.Height = minimapSize;

        _minimapRect.X = minimapX; _minimapRect.Y = minimapY;
        _minimapRect.Width = minimapSize; _minimapRect.Height = minimapSize;

        // Calculo de esquinas
        int cornerLength = (int)(minimapSize * 0.15f);
        int cornerThickness = Math.Max(2, (int)(minimapSize * 0.025f));
        int mX = minimapX, mY = minimapY, mW = minimapSize, mH = minimapSize;

        _minimapCorners[0].X = mX; _minimapCorners[0].Y = mY; _minimapCorners[0].Width = cornerLength; _minimapCorners[0].Height = cornerThickness;
        _minimapCorners[1].X = mX; _minimapCorners[1].Y = mY; _minimapCorners[1].Width = cornerThickness; _minimapCorners[1].Height = cornerLength;
        _minimapCorners[2].X = mX + mW - cornerLength; _minimapCorners[2].Y = mY; _minimapCorners[2].Width = cornerLength; _minimapCorners[2].Height = cornerThickness;
        _minimapCorners[3].X = mX + mW - cornerThickness; _minimapCorners[3].Y = mY; _minimapCorners[3].Width = cornerThickness; _minimapCorners[3].Height = cornerLength;
        _minimapCorners[4].X = mX; _minimapCorners[4].Y = mY + mH - cornerThickness; _minimapCorners[4].Width = cornerLength; _minimapCorners[4].Height = cornerThickness;
        _minimapCorners[5].X = mX; _minimapCorners[5].Y = mY + mH - cornerLength; _minimapCorners[5].Width = cornerThickness; _minimapCorners[5].Height = cornerLength;
        _minimapCorners[6].X = mX + mW - cornerLength; _minimapCorners[6].Y = mY + mH - cornerThickness; _minimapCorners[6].Width = cornerLength; _minimapCorners[6].Height = cornerThickness;
        _minimapCorners[7].X = mX + mW - cornerThickness; _minimapCorners[7].Y = mY + mH - cornerLength; _minimapCorners[7].Width = cornerThickness; _minimapCorners[7].Height = cornerLength;

        // --- CALCULO RESPONSIVE PARA SEGMENTOS Y GRID ---
        _segmentWidth = Math.Max(3, (int)(screenWidth * 0.005f));
        _segmentGap = Math.Max(1, (int)(screenWidth * 0.0015f));
        _gridSpacing = (int)(minimapSize * 0.2f); // Divide el minimapa en 5x5
    }

    private void DrawMinimapGrid()
    {
        int mX = _minimapRect.X;
        int mY = _minimapRect.Y;
        int mW = _minimapRect.Width;
        int mH = _minimapRect.Height;

        // Lineas Verticales
        for (int i = 1; i < 5; i++)
        {
            _tempRect.X = mX + i * _gridSpacing;
            _tempRect.Y = mY;
            _tempRect.Width = 1;
            _tempRect.Height = mH;
            _spriteBatch.Draw(_whitePixel, _tempRect, _colorGrid);
        }

        // Lineas Horizontales
        for (int i = 1; i < 5; i++)
        {
            _tempRect.X = mX;
            _tempRect.Y = mY + i * _gridSpacing;
            _tempRect.Width = mW;
            _tempRect.Height = 1;
            _spriteBatch.Draw(_whitePixel, _tempRect, _colorGrid);
        }

        // Cruz central (Radar Center)
        int centerX = mX + mW / 2;
        int centerY = mY + mH / 2;
        int crossSize = Math.Max(4, mW / 20);

        _tempRect.X = centerX - crossSize;
        _tempRect.Y = centerY;
        _tempRect.Width = crossSize * 2;
        _tempRect.Height = 1;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorGrid);

        _tempRect.X = centerX;
        _tempRect.Y = centerY - crossSize;
        _tempRect.Width = 1;
        _tempRect.Height = crossSize * 2;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorGrid);
    }

    private void DrawRightPanel()
    {
        float innerPadX = _rightPanelRect.Width * 0.08f;
        float currentY = _rightPanelRect.Y + _rightPanelRect.Height * 0.06f;

        // FUEL
        _spriteBatch.DrawString(_fontLabels, LABEL_FUEL, new Vector2(_rightPanelRect.X + innerPadX, currentY), _colorLabel);
        currentY += _fontLabels.LineSpacing * 0.9f;

        Color fuelColor = TankFuel > 30f ? Color.Lime : TankFuel > 10f ? Color.Yellow : Color.Red;
        _spriteBatch.DrawString(_fontNumbers, _cachedFuelValue, new Vector2(_rightPanelRect.X + innerPadX, currentY), fuelColor);
        currentY += _fontNumbers.LineSpacing * 1.1f;

        DrawBar(new Vector2(_rightPanelRect.X + innerPadX, currentY), fuelColor, TankFuel / 100f, _rightPanelRect.Width - innerPadX * 2);
        currentY += Math.Max(4, (int)(_rightPanelRect.Height * 0.04f)) + _rightPanelRect.Height * 0.06f;

        // HEALTH
        _spriteBatch.DrawString(_fontLabels, LABEL_HEALTH, new Vector2(_rightPanelRect.X + innerPadX, currentY), _colorLabel);
        currentY += _fontLabels.LineSpacing * 0.9f;

        float healthPercent = getPlayerHealth() / TGCGame.Instance._tank.initialHealth;
        Color healthColor = healthPercent > 0.5f ? Color.Lime : healthPercent > 0.25f ? Color.Yellow : Color.Red;
        _spriteBatch.DrawString(_fontNumbers, _cachedHealthValue, new Vector2(_rightPanelRect.X + innerPadX, currentY), healthColor);
        currentY += _fontNumbers.LineSpacing * 1.1f;

        DrawBar(new Vector2(_rightPanelRect.X + innerPadX, currentY), healthColor, healthPercent, _rightPanelRect.Width - innerPadX * 2);
    }

    private void DrawLeftPanel()
    {
        float innerPadX = _leftPanelRect.Width * 0.08f;
        float currentY = _leftPanelRect.Y + _leftPanelRect.Height * 0.08f;

        // KILLS
        _spriteBatch.DrawString(_fontLabels, LABEL_KILLS, new Vector2(_leftPanelRect.X + innerPadX, currentY), _colorLabel);
        currentY += _fontLabels.LineSpacing * 0.9f;
        _spriteBatch.DrawString(_fontNumbers, _cachedKillsValue, new Vector2(_leftPanelRect.X + innerPadX, currentY), Color.White);
        currentY += _fontNumbers.LineSpacing * 1.1f;

        // COOLDOWN
        _spriteBatch.DrawString(_fontLabels, LABEL_COOLDOWN, new Vector2(_leftPanelRect.X + innerPadX, currentY), _colorLabel);
        currentY += _fontLabels.LineSpacing * 0.9f;

        Color cooldownColor = CannonCurrentCooldown <= 0f ? Color.Lime : Color.Orange;
        _spriteBatch.DrawString(_fontNumbers, _cachedCooldownValue, new Vector2(_leftPanelRect.X + innerPadX, currentY), cooldownColor);
        currentY += _fontNumbers.LineSpacing * 1.1f;

        float barPercent = CannonMaxCooldown > 0f ? (1f - (MathHelper.Max(0f, CannonCurrentCooldown) / CannonMaxCooldown)) : 1f;
        DrawBar(new Vector2(_leftPanelRect.X + innerPadX, currentY), cooldownColor, barPercent, _leftPanelRect.Width - innerPadX * 2);
    }

    /// <summary>
    /// Dibuja una barra con efecto Inset (Profundidad) y Relleno Segmentado (Sci-Fi).
    /// 100% Zero-Allocation: Reutiliza _tempRect y colores en cache.
    /// </summary>
    private void DrawBar(Vector2 position, Color color, float percent, float maxWidth)
    {
        percent = MathHelper.Clamp(percent, 0f, 1f);
        int height = Math.Max(8, (int)(_rightPanelRect.Height * 0.05f));

        // 1. EFECTO BEVEL / INSET (Fondo hundido)
        // Borde exterior oscuro
        _tempRect.X = (int)position.X;
        _tempRect.Y = (int)position.Y;
        _tempRect.Width = (int)maxWidth;
        _tempRect.Height = height;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorBarOuter);

        // Piso interior ligeramente mas claro
        _tempRect.X += 1;
        _tempRect.Y += 1;
        _tempRect.Width -= 2;
        _tempRect.Height -= 2;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorBarInner);

        // 2. RELLENO SEGMENTADO (Estilo Baterias)
        int totalSegmentSize = _segmentWidth + _segmentGap;
        int availableWidth = (int)maxWidth - 2; // Restars el borde inset
        int totalSegments = availableWidth / totalSegmentSize;
        int filledSegments = (int)(percent * totalSegments);

        int startX = (int)position.X + 1;
        int startY = (int)position.Y + 1;
        int fillHeight = height - 2;

        for (int i = 0; i <= totalSegments; i++)
        {
            _tempRect.X = startX + i * totalSegmentSize;
            _tempRect.Y = startY;
            _tempRect.Width = _segmentWidth;
            _tempRect.Height = fillHeight;

            if (i <= filledSegments)
            {
                _spriteBatch.Draw(_whitePixel, _tempRect, color); // Segmento activo
            }
            else
            {
                _spriteBatch.Draw(_whitePixel, _tempRect, _colorSegmentEmpty); // Segmento vacio sutil
            }
        }

        // CIERRE DERECHO: Linea vertical de 1px para cerrar el ultimo casillero
        _tempRect.X = (int)position.X + (int)maxWidth - 1;
        _tempRect.Y = (int)position.Y + 1;
        _tempRect.Width = 1;
        _tempRect.Height = height - 2;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorBarOuter);
    }

    private void DrawMinimapIcons()
    {
        Vector2 playerMarker = PositionWorldToMinimap(TankPosition, _minimapRect);
        float tankRotation = -TankRotation;
        _spriteBatch.Draw(_playerTexture, playerMarker, null, Color.Lime, tankRotation,
            new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);

        foreach (var enemyPos in EnemyPositions)
        {
            Vector2 enemyMarker = PositionWorldToMinimap(enemyPos, _minimapRect);
            if (_minimapRect.Contains((int)enemyMarker.X, (int)enemyMarker.Y))
            {
                _spriteBatch.Draw(_enemyTexture, enemyMarker, null, Color.Red, 0f,
                    new Vector2(_enemyTexture.Width / 2f, _enemyTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);
            }
        }

        foreach (var barrelPos in FuelPositions)
        {
            Vector2 barrelMarker = PositionWorldToMinimap(barrelPos, _minimapRect);
            if (_minimapRect.Contains((int)barrelMarker.X, (int)barrelMarker.Y))
            {
                _spriteBatch.Draw(_fuelTexture, barrelMarker, null, Color.Yellow, 0f,
                    new Vector2(_fuelTexture.Width / 2f, _fuelTexture.Height / 2f), 0.5f, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawLegend()
    {
        int legendY = _minimapRect.Bottom + (int)(_lastScreenHeight * 0.015f);
        int legendHeight = (int)(_lastScreenHeight * 0.055f);

        _tempRect.X = _minimapRect.X;
        _tempRect.Y = legendY;
        _tempRect.Width = _minimapRect.Width;
        _tempRect.Height = legendHeight;
        _spriteBatch.Draw(_whitePixel, _tempRect, _colorPanelBg);

        float iconScale = 0.35f;
        float iconY1 = legendY + legendHeight * 0.3f;
        float iconY2 = legendY + legendHeight * 0.75f;
        float textOffsetX = _minimapRect.X + 35;

        _spriteBatch.Draw(_enemyTexture, new Vector2(_minimapRect.X + 18, iconY1), null, Color.Red, 0f,
            new Vector2(_enemyTexture.Width / 2f, _enemyTexture.Height / 2f), iconScale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_fontLabels, "Enemigo", new Vector2(textOffsetX, iconY1 - 10), Color.White);

        _spriteBatch.Draw(_fuelTexture, new Vector2(_minimapRect.X + 18, iconY2), null, Color.Yellow, 0f,
            new Vector2(_fuelTexture.Width / 2f, _fuelTexture.Height / 2f), iconScale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_fontLabels, "Combustible", new Vector2(textOffsetX, iconY2 - 10), Color.White);
    }

    private int getPlayerHealth() => (int)TGCGame.Instance._tank.HealthPoints;

    private Vector2 PositionWorldToMinimap(Vector3 worldPos, Rectangle mapRect)
    {
        float nx = MathHelper.Clamp((worldPos.X + WidthUnits) / (WidthUnits * 2f), 0f, 1f);
        float nz = MathHelper.Clamp((worldPos.Z + HeightUnits) / (HeightUnits * 2f), 0f, 1f);
        float x = mapRect.X + nx * mapRect.Width;
        float y = mapRect.Y + nz * mapRect.Height;

        const float markerMargin = 8f;
        x = MathHelper.Clamp(x, mapRect.Left + markerMargin, mapRect.Right - markerMargin);
        y = MathHelper.Clamp(y, mapRect.Top + markerMargin, mapRect.Bottom - markerMargin);
        return new Vector2(x, y);
    }

    #endregion

    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
    }
}