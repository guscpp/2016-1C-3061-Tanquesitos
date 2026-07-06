using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models.Particulas;

public class SmokeParticle : Particle {
    public void Initialize(Vector3 position, Vector3 direction, float time)
    {
        genTime = time;
        _initialPos = position;
        _currentPos = position;
        _size = 0.5f;
        _isAlive = true;
        speed = 0.5f; 
        Velocity = Vector3.Normalize(Vector3.Up + direction) * speed;
        _color = Color.Gray;
        maxTime = 1.2f;
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
        _alpha = 1 - lifePercent;
        _currentPos += Velocity * dt;
    }
}