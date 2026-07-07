﻿using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models.Tanks;

public class TankEnemyScout : TankEnemy
{
    public TankEnemyScout(GraphicsDevice graphicsDevice) : base(
        graphicsDevice: graphicsDevice,
        GameConfig.TankClass.Scout,
        hp: GameConfig.TankClasses.Scout.EnemyHealth,
        speed: GameConfig.TankClasses.Scout.MaxSpeed,
        force: GameConfig.TankClasses.Scout.MotorForce,
        turnSpeed: GameConfig.TankClasses.Scout.TurnSpeed,
        damage: GameConfig.TankClasses.Scout.AttackDamage,
        cooldown: GameConfig.TankClasses.Scout.Cooldown)
    { }
}

public class TankEnemyMedium : TankEnemy
{
    public TankEnemyMedium(GraphicsDevice graphicsDevice) : base(
        graphicsDevice: graphicsDevice,
        GameConfig.TankClass.Medium,
        hp: GameConfig.TankClasses.Medium.EnemyHealth,
        speed: GameConfig.TankClasses.Medium.MaxSpeed,
        force: GameConfig.TankClasses.Medium.MotorForce,
        turnSpeed: GameConfig.TankClasses.Medium.TurnSpeed,
        damage: GameConfig.TankClasses.Medium.AttackDamage,
        cooldown: GameConfig.TankClasses.Medium.Cooldown)
    { }
}

public class TankEnemyHeavy : TankEnemy
{
    public TankEnemyHeavy(GraphicsDevice graphicsDevice) : base(
        graphicsDevice: graphicsDevice,
        GameConfig.TankClass.Heavy,
        hp: GameConfig.TankClasses.Heavy.EnemyHealth,
        speed: GameConfig.TankClasses.Heavy.MaxSpeed,
        force: GameConfig.TankClasses.Heavy.MotorForce,
        turnSpeed: GameConfig.TankClasses.Heavy.TurnSpeed,
        damage: GameConfig.TankClasses.Heavy.AttackDamage,
        cooldown: GameConfig.TankClasses.Heavy.Cooldown)
    { }
}