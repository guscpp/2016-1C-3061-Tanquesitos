using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Models.Tanks;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using TGC.MonoGame.TP.Models.Particulas;

namespace TGC.MonoGame.TP.Managers;

public class ParticlesManager
{
    private Effect _effect;
    private ParticlesPool pool = new();
    private Texture2D particleTexture;
    private Random random = new();

    public ParticlesManager(Effect texturesEffect, Texture2D texture)
    {
        _effect = texturesEffect;
        particleTexture = texture;
        pool.Initialize();
    }

    public void GenerateSmoke(Vector3 from, Vector3 direction, float currentTime)
    {
        var particleCount = random.Next(1,4);
        for(int i=0; i<particleCount; i++)
        {
            var particula = pool.GetParticle(ParticlesPool.ParticleType.HUMO);
            if(particula is SmokeParticle smoke)
                smoke.Initialize(from, direction, currentTime);
        }
    }

    public void GenerateFire(Vector3 from, float currentTime)
    {
        var particleCount = random.Next(1,4);
        for(int i=0; i<particleCount; i++)
        {
            var particula = pool.GetParticle(ParticlesPool.ParticleType.FUEGO);
            if(particula is ExplosionParticle fire) 
                fire.Initialize(from, currentTime);
        }
    }

    public void GenerateDust(Vector3 from, Vector3 direction, float currentTime)
    {
        var particleCount = random.Next(1,4);
        for(int i=0; i<particleCount; i++)
        {
            var particula = pool.GetParticle(ParticlesPool.ParticleType.POLVO);
            if(particula is DustParticle dust) {
                dust.Initialize(from, direction, currentTime);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        var max = pool.MaxParticles;
        for(int i=0; i<max; i++)
        {
            if(pool._particulasExplosion[i].IsAlive) pool._particulasExplosion[i].Update(gameTime);
            if(pool._particulasHumo[i].IsAlive) pool._particulasHumo[i].Update(gameTime);
            if(pool._particulasPolvo[i].IsAlive) pool._particulasPolvo[i].Update(gameTime);
        }
    }

    public void Draw(Matrix View, Matrix Projection)
    {
        var max = pool.MaxParticles;
        for(int i=0; i<max; i++)
        {
            if(pool._particulasExplosion[i].IsAlive) pool._particulasExplosion[i].Draw(View, Projection, _effect, particleTexture);
            if(pool._particulasHumo[i].IsAlive) pool._particulasHumo[i].Draw(View, Projection, _effect, particleTexture);
            if(pool._particulasPolvo[i].IsAlive) pool._particulasPolvo[i].Draw(View, Projection, _effect, particleTexture);
        }
    }

    public void Reset()
    {
        var max = pool.MaxParticles;
        for(int i=0; i<max; i++)
        {
            if(pool._particulasExplosion[i].IsAlive) pool._particulasExplosion[i].Reset();
            if(pool._particulasHumo[i].IsAlive) pool._particulasHumo[i].Reset();
            if(pool._particulasPolvo[i].IsAlive) pool._particulasPolvo[i].Reset();
        }
    }

}