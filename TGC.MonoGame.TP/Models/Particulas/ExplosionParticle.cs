using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models.Particulas;

public class ExplosionParticle : Particle
{
    public void Initialize(Vector3 position, float time)
    {
        genTime = time;
        _initialPos = position;
        _currentPos = position;
        _isAlive = true;
        speed = 0.5f;
        float x = (random.NextSingle() - 0.5f) * 2f;
        float y = random.NextSingle() * 2f;
        float z = (random.NextSingle() - 0.5f) * 2f;
        Velocity = Vector3.Normalize(new Vector3(x, y, z)) * 3f;
        maxTime = 1f;
    }

    public void Update(GameTime gameTime)
    {
        var totaltime = (float)gameTime.TotalGameTime.TotalSeconds;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!_isAlive) return;

        var currentTime = totaltime - genTime;
        if (currentTime >= maxTime) 
        { 
            Reset(); 
            return; 
        }
        var lifePercent = currentTime / maxTime;
        _alpha = 1f - lifePercent;
        _size = MathHelper.Lerp(1.5f, 0f, lifePercent);
        _color = Color.Lerp(Color.OrangeRed, Color.Black, lifePercent);

        Velocity += new Vector3(0, -9.8f, 0) * dt;
        _currentPos += Velocity * dt;
    }
}