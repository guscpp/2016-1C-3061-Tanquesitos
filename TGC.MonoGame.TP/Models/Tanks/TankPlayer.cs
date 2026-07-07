using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using System.Runtime.Serialization.DataContracts;

namespace TGC.MonoGame.TP.Models.Tanks;

public class TankPlayer : TankBase
{
    public float CurrentFuel { get; private set; } = GameConfig.Tank.MaxFuel;
    public float initialHealth { get; }

    public TankPlayer(GraphicsDevice graphicsDevice, GameConfig.TankClass tankClass)
    {
        _graphicsDevice = graphicsDevice;

        TankClass = tankClass;

        switch (tankClass)
        {
            case GameConfig.TankClass.Scout:
                HealthPoints = GameConfig.TankClasses.Scout.PlayerHealth;
                initialHealth = HealthPoints;
                MaxSpeed = GameConfig.TankClasses.Scout.MaxSpeed;
                MotorForce = GameConfig.TankClasses.Scout.MotorForce;
                TurnSpeed = GameConfig.TankClasses.Scout.TurnSpeed;
                AttackDamage = GameConfig.TankClasses.Scout.AttackDamage;
                break;

            case GameConfig.TankClass.Heavy:
                HealthPoints = GameConfig.TankClasses.Heavy.PlayerHealth;
                initialHealth = HealthPoints;
                MaxSpeed = GameConfig.TankClasses.Heavy.MaxSpeed;
                MotorForce = GameConfig.TankClasses.Heavy.MotorForce;
                TurnSpeed = GameConfig.TankClasses.Heavy.TurnSpeed;
                AttackDamage = GameConfig.TankClasses.Heavy.AttackDamage;
                break;

            case GameConfig.TankClass.Medium:
            default:
                HealthPoints = GameConfig.TankClasses.Medium.PlayerHealth;
                initialHealth = HealthPoints;
                MaxSpeed = GameConfig.TankClasses.Medium.MaxSpeed;
                MotorForce = GameConfig.TankClasses.Medium.MotorForce;
                TurnSpeed = GameConfig.TankClasses.Medium.TurnSpeed;
                AttackDamage = GameConfig.TankClasses.Medium.AttackDamage;
                break;
        }

        // Propiedades comunes a todos los tanques del jugador
        ForwardDrag = GameConfig.Tank.ForwardDrag;
        LateralDrag = GameConfig.Tank.LateralDrag;
        CurrentFuel = GameConfig.Tank.MaxFuel;
    }

    public override void HandleHealth(float damage, Vector3 impactPointWorld)
    {
        //if (TGCGame.Instance.GameStateManager.IsGodMode) return;

        if (damage > 0 && !IsDead)
            TGCGame.Instance.Camera.Shake(GameConfig.Camera.ShakeIntensity, GameConfig.Camera.ShakeDuration);

        base.HandleHealth(damage, impactPointWorld);
    }

    public void AddFuel(float amount) => CurrentFuel = MathHelper.Clamp(CurrentFuel + amount, 0f, GameConfig.Tank.MaxFuel);

    public void Update(GameTime gameTime, KeyboardState keyboard, Simulation simulation)
    {
        if (IsDead) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        if (CurrentFuel <= 0f && !TGCGame.Instance.GameStateManager.IsGodMode)
        {
            forwardInput = 0f;
            IsDead = true;
        }
        else if (forwardInput != 0f && !TGCGame.Instance.GameStateManager.IsGodMode)
            CurrentFuel -= GameConfig.Tank.FuelConsumptionRate * dt;
        CurrentFuel = MathHelper.Clamp(CurrentFuel, 0f, GameConfig.Tank.MaxFuel);

        float turnInput = 0f;
        if (keyboard.IsKeyDown(Keys.A)) turnInput += 1f;
        if (keyboard.IsKeyDown(Keys.D)) turnInput -= 1f;

        // Control de torreta con mouse
        if (TGCGame.Instance.IsActive)
        {
            TGCGame.Instance.IsMouseVisible = false;
            var ms = Mouse.GetState();
            int cx = TGCGame.Instance.GraphicsDevice.Viewport.Width / 2;
            int cy = TGCGame.Instance.GraphicsDevice.Viewport.Height / 2;
            _turretRotation -= (ms.X - cx) * 0.0015f;
            _cannonRotation -= (ms.Y - cy) * 0.0015f;
            _cannonRotation = MathHelper.Clamp(_cannonRotation, _minCannonPitch, _maxCannonPitch);
            Mouse.SetPosition(cx, cy);
        }
        else TGCGame.Instance.IsMouseVisible = true;

        ApplyPhysics(simulation, dt, forwardInput, turnInput);

        if(forwardInput != 0)
        {
            // uno para cada oruga =D
            var dustSpawnPos = Position + Vector3.Down * 0.4f + new Vector3(0,0,1);
            var dustSpawnPos2 = Position + Vector3.Down * 0.4f + new Vector3(0,0,-1);
            TGCGame.Instance.ParticlesManager.GenerateDust(dustSpawnPos, ChassisRight, (float)gameTime.TotalGameTime.TotalSeconds);
            TGCGame.Instance.ParticlesManager.GenerateDust(dustSpawnPos2, ChassisRight, (float)gameTime.TotalGameTime.TotalSeconds);
        }
        RecalculateMatrices();

        RecalculateWorldBoundingBox();
    }
}