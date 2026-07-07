using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Managers;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models.Particulas;

public class SmokeParticle : Particle {
    private float _startSize;
    private float _endSize;
    public void Initialize(Vector3 position, Vector3 direction, float time)
    {
        genTime = time;
        _initialPos = position;
        _currentPos = position;
        _isAlive = true;

        _startSize = MathHelper.Lerp(0.08f, 0.5f, random.NextSingle());
        _endSize = _startSize + MathHelper.Lerp(0.3f, 0.8f, random.NextSingle());
        _size = _startSize;

        Vector3 randomSpread = new Vector3((random.NextSingle() - 0.5f) * 0.6f, 0f, (random.NextSingle() - 0.5f) * 0.6f);
        speed = MathHelper.Lerp(0.3f, 0.8f, random.NextSingle());
        Velocity = Vector3.Normalize(Vector3.Up + direction + randomSpread) * speed;
        _color = Color.LightGray;
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
        _size = MathHelper.Lerp(_startSize, _endSize, lifePercent);
        // igual q el polvo, se va relentizando + desapareciendo
        Velocity *= (1f - 1.5f * dt);
        _currentPos += Velocity * dt;
    }
}