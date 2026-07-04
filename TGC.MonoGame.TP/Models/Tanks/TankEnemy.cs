using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;

namespace TGC.MonoGame.TP.Models.Tanks;

public abstract class TankEnemy : TankBase
{
    //Estados de los enemigos
    public enum EnemyState { Patrol, Attack, Flee, Dead }
    protected EnemyState currentState = EnemyState.Patrol;

    private float _patrolTimer = 0f;
    private float _patrolDirection = 1f;

    protected Vector3 _targetPosition;
    private float attackRadius = GameConfig.Enemies.AttackRadius;

    protected float MaxHealthPoints { get; private set; }
    protected float _currentShootCooldown = 0f;
    public float ShootCooldown { get; protected set; }

    protected TankEnemy(GraphicsDevice graphicsDevice, GameConfig.TankClass tankClass, float hp, float speed, float force, float turnSpeed, float damage, float cooldown)
    {
        _graphicsDevice = graphicsDevice;
        TankClass = tankClass;
        HealthPoints = hp;
        MaxHealthPoints = hp;
        MaxSpeed = speed;
        MotorForce = force;
        TurnSpeed = turnSpeed;
        ForwardDrag = GameConfig.Tank.ForwardDrag;
        LateralDrag = GameConfig.Tank.LateralDrag;
        AttackDamage = damage;
        ShootCooldown = cooldown;
    }

    public override void HandleHealth(float damage, Vector3 impactPointWorld)
    {
        base.HandleHealth(damage, impactPointWorld);

        float remainingHP = Math.Max(0, HealthPoints);

        if (TGCGame.Instance.Hud != null) TGCGame.Instance.Hud.AddDamageNumber(impactPointWorld, remainingHP);
    }

    // Mantengo la firma original para no tocar TGCGame.cs
    public void UpdateEnemy(GameTime gameTime, Simulation simulation, Vector3 targetPos)
    {
        if (IsDead) { return;}

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _targetPosition = targetPos;
        _currentShootCooldown -= dt;

        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var currentPos = new Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
        var toTarget = _targetPosition - currentPos;
        float distanceToPlayer = toTarget.Length();

        //Transiciones de estados
        if (HealthPoints <= 0)
        {
            currentState = EnemyState.Dead;
            IsDead = true;
            return;
        }
        else if (HealthPoints < MaxHealthPoints * 0.25f)
        {
            currentState = EnemyState.Flee;
        }
        else if (distanceToPlayer <= attackRadius)
        {
            currentState = EnemyState.Attack;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }

        //Para orientarse segun el jugador
        var orientation = body.Pose.Orientation;
        var forward = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 0, -1), orientation);
        var forwardXna = new Vector3(forward.X, forward.Y, forward.Z);

        //Ejecutar comportamiento segun el estado
        float forwardInput = 0f;
        float turnInput = 0f;

        switch (currentState)
        {
            case EnemyState.Patrol:
                //Avanzar despacio y girar aleatoriamente cada cierto tiempo
                _patrolTimer -= dt;
                if (_patrolTimer <= 0)
                {
                    _patrolDirection = new Random().Next(-1, 2); // -1, 0, o 1
                    _patrolTimer = new Random().NextSingle() * 3f + 1f; // Cambiar entre 1 y 4 segundos
                }
                forwardInput = 0.7f; // Velocidad reducida (30%)
                turnInput = _patrolDirection * 0.5f; // Giro suave
                break;

            case EnemyState.Attack:
                // Apuntarle al jugador
                float worldAngleToPlayer = MathF.Atan2(-toTarget.X, -toTarget.Z);
                _turretRotation = worldAngleToPlayer - RotationY;
                _cannonRotation = MathHelper.Clamp(-toTarget.Y * 0.015f, _minCannonPitch, _maxCannonPitch);

                float angleToTarget = MathF.Atan2(
                    Vector3.Cross(forwardXna, toTarget).Y,
                    Vector3.Dot(forwardXna, toTarget)
                );

                turnInput = Math.Sign(angleToTarget) * Math.Min(MathF.Abs(angleToTarget), 1f);
                forwardInput = (distanceToPlayer > GameConfig.Enemies.AttackStopDistance) ? 1f : 0f; // Detenerse si esta muy lejos

                // Disparar
                if (_currentShootCooldown <= 0f && distanceToPlayer > GameConfig.Enemies.AttackFireDistance)
                {
                    FireCannon(currentPos);
                    _currentShootCooldown = ShootCooldown;
                }
                break;

            case EnemyState.Flee:
                //Apuntar en direccion opuesta y acelerar
                float fleeAngle = MathF.Atan2(toTarget.X, toTarget.Z); // Notar el signo invertido respecto al ataque
                _turretRotation = fleeAngle - RotationY; // Mirar hacia atras mientras huye

                // Calcular para darse vuelta y huir
                var fleeDir = -toTarget;
                fleeDir.Normalize();
                float angleToFlee = MathF.Atan2(
                    Vector3.Cross(forwardXna, fleeDir).Y,
                    Vector3.Dot(forwardXna, fleeDir)
                );

                turnInput = Math.Sign(angleToFlee) * Math.Min(MathF.Abs(angleToFlee), 1f);
                forwardInput = 1f;
                break;

            case EnemyState.Dead:
                forwardInput = 0f;
                turnInput = 0f;
                break;
        }

        //Aplicar fisica
        ApplyPhysics(simulation, dt, forwardInput, turnInput);
        RecalculateMatrices();
        RecalculateWorldBoundingBox();
    }

    //Metodo auxiliar
    private void FireCannon(Vector3 currentPos)
    {
        var dir = CannonForward;
        var spawnPos = currentPos + dir * GameConfig.Enemies.CannonSpawnOffsetForward +
            Vector3.Up * GameConfig.Enemies.CannonSpawnOffsetUp;

        float enemyPitch = TankClass switch
        {
            GameConfig.TankClass.Scout => GameConfig.TankClasses.Scout.CannonPitch,
            GameConfig.TankClass.Heavy => GameConfig.TankClasses.Heavy.CannonPitch,
            _ => GameConfig.TankClasses.Medium.CannonPitch
        };

        TGCGame.Instance.CannonballManager.Fire(
            spawnPos,
            dir,
            AttackDamage,
            TGCGame.Instance.SoundManager,
            TGCGame.Instance.Camera.ListenerPosition,
            TGCGame.Instance.Camera.ListenerForward,
            false,
            enemyPitch);
    }

    // Posicion inicial aleatoria para spawnear(sin cambios)
    public Vector3 GetPosition(Terrain terrain, Random random)
    {
        var min = -terrain.WidthUnits - GameConfig.Enemies.SpawnMapMargin;
        var max = terrain.WidthUnits - GameConfig.Enemies.SpawnMapMargin;
        var x = random.NextSingle() * (max - min) + min;
        var z = random.NextSingle() * (max - min) + min;
        return new Vector3(x, terrain.GetHeight(x, z) + GameConfig.Tank.SpawnZMargin, z);
    }
}