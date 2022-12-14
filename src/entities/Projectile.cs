using Godot;

public class Projectile : PhysicsObject
{
    [Export]
    public float Lifetime = 10;
    [Export]
    public float ExplosionSize = 10;
    [Export]
    public float FlingFactor = 1;

    private bool dying = false;
    private bool exploded = false;
    private float dieTime = 2;

    public void Launch(Vector3 velocity)
    {
        Velocity = velocity;
        LookAt(Position + velocity, Vector3.Up);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        Lifetime -= delta;
        if (Lifetime <= 0) {
            QueueFree();
        }
        if (!exploded && collisionDirections != 0)
        {
            Explode();
        }
        if (dying)
        {
            dieTime -= delta;
            if (dieTime <= 0) {
                QueueFree();
            }
        }
    }

    public void Explode()
    {
        exploded = true;
        SphereShaper.Shape(World.Singleton, Position, ExplosionSize);
        Particles TrailParticles = GetNode<Particles>("Trail");
        Particles ExplosionParticles = GetNode<Particles>("Explosion");
        ExplosionParticles.Emitting = true;
        TrailParticles.Emitting = false;
        PhysicsActive = false;
        dying = true;
        foreach(PhysicsObject obj in World.Singleton.PhysicsObjects)
        {
            if (obj == this) return;
            float mag = (obj.Position-Position).LengthSquared()+1;
            obj.Velocity += ((obj.Position-Position)/mag*ExplosionSize*FlingFactor);
        }
    }


}