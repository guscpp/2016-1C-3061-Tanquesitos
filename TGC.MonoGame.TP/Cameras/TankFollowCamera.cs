using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using TGC.MonoGame.TP.Models.Terrains;

namespace TGC.MonoGame.TP.Cameras;

/// <summary>
///     Camara en tercera persona que sigue al tanque desde atras y arriba.
///     Cuando el tanque recibe un disparo tiene camera-shake
/// </summary>
public class TankFollowCamera
{
    //configuracion de la camara
    public float Distance { get; set; } = GameConfig.Camera.DefaultDistance;
    public float Smoothness { get; set; } = GameConfig.Camera.Smoothness;

    //configuracion del zoom
    public float MinDistance { get; set; } = GameConfig.Camera.MinDistance;
    public float MaxDistance { get; set; } = GameConfig.Camera.MaxDistance;
    public float ZoomSensitivity { get; set; } = GameConfig.Camera.ZoomSensitivity;

    //para calcular el 3d del audio 3d
    public Vector3 ListenerPosition => _currentPosition;
    public Vector3 ListenerForward => Vector3.Normalize(_lookAt - _currentPosition);

    //para efectos
    public Terrain Terrain { get; set; } //referencia al terreno para clamping de altura
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeTimer = 0f;
    private readonly Random _random = new Random();

    private Vector3 _currentPosition;
    private Vector3 _targetPosition;
    private Vector3 _lookAt;
    private int _lastScrollValue;

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }

    /// <summary>
    ///  Inicia el efecto camera-shake
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }


    public TankFollowCamera(float aspectRatio, Vector3 initialTankPos)
    {
        _currentPosition = initialTankPos + Vector3.Backward * Distance + Vector3.Up * GameConfig.Camera.DefaultHeightOffset;
        _targetPosition = _currentPosition;
        _lookAt = initialTankPos + Vector3.Up * GameConfig.Camera.DefaultLookAtHeight;

        UpdateProjection(aspectRatio);
        UpdateView();
    }

    public void UpdateProjection(float aspectRatio)
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, 
            aspectRatio, 
            GameConfig.Camera.NearPlaneDist, 
            GameConfig.Camera.FarPlaneDist
        );
    }

    public void Update(GameTime gameTime, Vector3 tankPosition, float tankRotationY)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. Zoom in/out
        var mouseState = Mouse.GetState();
        int scrollDelta = mouseState.ScrollWheelValue - _lastScrollValue;
        if (scrollDelta != 0)
        {
            Distance -= scrollDelta / 120f * ZoomSensitivity;
            Distance = MathHelper.Clamp(Distance, MinDistance, MaxDistance);
        }
        _lastScrollValue = mouseState.ScrollWheelValue;

        // 2. Direcciones
        Vector3 tankForward = Vector3.TransformNormal(Vector3.Forward, Matrix.CreateRotationY(tankRotationY));
        Vector3 tankBackward = -tankForward;

        // 3. Factor de transición (0 = Normal, 1 = Escotilla)
        float hatchFactor = 0f;
        float transitionDist = GameConfig.Camera.HatchTransitionDistance;

        if (Distance < transitionDist)
        {
            float range = transitionDist - MinDistance;
            float progress = transitionDist - Distance;
            hatchFactor = MathHelper.Clamp(progress / range, 0f, 1f);
            hatchFactor = MathHelper.SmoothStep(0f, 1f, hatchFactor);
        }

        // 4. Interpolar ALTURA DE CÁMARA
        float currentHeight = MathHelper.Lerp(GameConfig.Camera.DefaultHeightOffset, GameConfig.Camera.HatchHeightOffset, hatchFactor);

        // Si estamos en modo comandante (hatchFactor > 0), el LookAt debe estar a la MISMA altura que la cámara.
        float currentLookAtHeight = MathHelper.Lerp(GameConfig.Camera.DefaultLookAtHeight, currentHeight, hatchFactor);

        // 6. Desplazamiento hacia adelante
        float currentLookAtForward = MathHelper.Lerp(0f, GameConfig.Camera.HatchLookAtForward, hatchFactor);

        float currentCameraForwardOffset = MathHelper.Lerp(0f, GameConfig.Camera.HatchPositionForwardOffset, hatchFactor);

        // 7. Calcular Posición y Punto de Mira
        _targetPosition = tankPosition 
            + tankBackward * Distance 
            + tankForward * currentCameraForwardOffset
            + Vector3.Up * currentHeight;
        _lookAt = tankPosition + tankForward * currentLookAtForward + Vector3.Up * currentLookAtHeight;

        // 8. Suavizar
        _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, dt * Smoothness);

        // === Camera shake ===
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            float currentIntensity = _shakeIntensity * (_shakeTimer / _shakeDuration);
            float shakeX = (float)(_random.NextDouble() * 2.0 - 1.0) * currentIntensity;
            float shakeY = (float)(_random.NextDouble() * 2.0 - 1.0) * currentIntensity;
            float shakeZ = (float)(_random.NextDouble() * 2.0 - 1.0) * currentIntensity;
            _currentPosition += new Vector3(shakeX, shakeY, shakeZ);
        }

        // === Camera clamping ===
        if (Terrain != null)
        {
            float terrainHeight = Terrain.GetHeight(_currentPosition.X, _currentPosition.Z);
            float minHeight = terrainHeight + GameConfig.Camera.TerrainClampOffset;
            if (_currentPosition.Y < minHeight) _currentPosition.Y = minHeight;
        }

        UpdateView();
    }

    private void UpdateView()
    {
        View = Matrix.CreateLookAt(_currentPosition, _lookAt, Vector3.Up);
    }
}