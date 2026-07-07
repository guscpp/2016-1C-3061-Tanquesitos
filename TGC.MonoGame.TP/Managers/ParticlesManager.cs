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
    public Texture2D sandTexture;
    public Texture2D smokeTexture;
    public Texture2D explosionTexture;
    private Random random = new();

    public ParticlesManager(Effect texturesEffect)
    {
        _effect = texturesEffect;
        pool.Initialize();
    }

    public void Initialize(ContentManager content)
    {
        sandTexture = content.Load<Texture2D>("Textures/particula_100x100");
        smokeTexture = content.Load<Texture2D>("Textures/particle-smoke");
        explosionTexture = content.Load<Texture2D>("Textures/particula_explosion");
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
            if(pool._particulasExplosion[i].IsAlive) pool._particulasExplosion[i].Draw(View, Projection, _effect, explosionTexture);
            if(pool._particulasHumo[i].IsAlive) pool._particulasHumo[i].Draw(View, Projection, _effect, smokeTexture);
            if(pool._particulasPolvo[i].IsAlive) pool._particulasPolvo[i].Draw(View, Projection, _effect, sandTexture);
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