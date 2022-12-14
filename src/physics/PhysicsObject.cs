using Godot;
using System.Runtime.CompilerServices;

public class PhysicsObject : Spatial
{
    [Export]
    public Vector3 Size;
    [Export]
    //reduces collision planes' sizes by this amount in each direction to avoid getting stuck on things.
    //For example, it would stop an x-axis collision to be detected while walking on flat ground.
    public float Epsilon = 0.1f;
    public Vector3 Velocity;
    public Vector3 Position {
        get => GlobalTransform.origin;
        set {GlobalTransform = new Transform(GlobalTransform.basis, value);}
    }
    [Export]
    public Vector3 Gravity = new Vector3(0,-10,0);
    [Export]
    public bool PhysicsActive = true;

    protected Vector3 currentForce; //zeroed each physics update
    protected int collisionDirections = 0; //updated each physics update, bitmask of Directions of current collision with world

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box GetBox() => Box.FromCenter(Position, Size);

    public override void _EnterTree()
    {
        base._EnterTree();
        World.Singleton.PhysicsObjects.Add(this);
    }

    public override void _PhysicsProcess(float dt)
    {
        if (!PhysicsActive) return;
        Velocity += currentForce*dt;
        //GD.Print(Velocity);
        currentForce = Gravity;
        doCollision(World.Singleton, dt);

        Position += Velocity*dt;
    }

    public override void _ExitTree()
    {
        World.Singleton.PhysicsObjects.Remove(this);
        base._ExitTree();
    }

    //adds a force for the next physics update, does not persist across updates
    //world space
    public void AddForce(Vector3 f)
    {
        currentForce += f;
    }

    //rotates v from local to world
    public Vector3 LocalDirectionToWorld(Vector3 v)
    {
        return GlobalTransform.basis.Xform(v);
    }
    protected void doCollision(World world, float dt)
    {
        Vector3 oldV = Velocity;
        int oldMask = collisionDirections;
        collisionDirections = 0;
        Vector3 frameVelocity = Velocity * dt; //this is the amount the object will move this update
        Vector3 postPosition = Position;
        //we're going to check collision by iterating through each plane orthogonal to the Velocity the the three axes directions.
        //we only have to check each plane of blocks that the frame Velocity vector hits.

        //y axis
        if (frameVelocity.y < 0)
        {
            //moving down
            for (int y = 0; y > Mathf.FloorToInt(frameVelocity.y); y--)
            {
                if (Plane.CollidesWithWorldY(Position + new Vector3(-Size.x/2,-Size.y/2+y, -Size.z/2), Size.x, Size.z, world))
                {
                    postPosition.y = Mathf.CeilToInt(Position.y-Size.y/2+y)+Size.y/2;
                    Velocity.y = 0;
                    collisionDirections |= (1<<(int)Direction.NegY);
                    break;
                }
            }
        }
        else
        {
            //moving up
            for (int y = 0; y < Mathf.CeilToInt(frameVelocity.y); y++)
            {
                if (Plane.CollidesWithWorldY(Position + new Vector3(-Size.x/2,Size.y/2+y, -Size.z/2), Size.x, Size.z, world))
                {
                    postPosition.y = Mathf.FloorToInt(Position.y+Size.y/2+y)-Size.y/2;
                    Velocity.y = 0;
                    collisionDirections |= (1<<(int)Direction.PosY);
                    break;
                }
            }
        }

        //x axis
        if (frameVelocity.x < 0)
        {
            //moving left
            for (int x = 0; x > Mathf.FloorToInt(frameVelocity.x); x--)
            {
                if (Plane.CollidesWithWorldX(Position + new Vector3(-Size.x/2+x,-Size.y/2+Epsilon, -Size.z/2+Epsilon), Size.y-2*Epsilon, Size.z-2*Epsilon, world))
                {
                    //postPosition.x = Mathf.CeilToInt(Position.x-Size.x/2+x)+Size.x/2;
                    Velocity.x = 0;
                    collisionDirections |= (1<<(int)Direction.NegX);
                    break;
                }
            }
        }
        else
        {
            //moving right
            for (int x = 0; x < Mathf.CeilToInt(frameVelocity.x); x++)
            {
                if (Plane.CollidesWithWorldX(Position + new Vector3(Size.x/2+x,-Size.y/2+Epsilon, -Size.z/2+Epsilon), Size.y-2*Epsilon, Size.z-2*Epsilon, world))
                {
                    postPosition.x = Mathf.FloorToInt(Position.x+Size.x/2+x)-Size.x/2;
                    Velocity.x = 0;
                    collisionDirections |= (1<<(int)Direction.PosX);
                    break;
                }
            }
        }
        //z azis
        if (frameVelocity.z < 0)
        {
            //moving forward
            for (int z = 0; z > Mathf.FloorToInt(frameVelocity.z); z--)
            {
                if (Plane.CollidesWithWorldZ(Position + new Vector3(-Size.z/2+Epsilon,-Size.y/2+Epsilon, -Size.z/2+z), Size.x-2*Epsilon,Size.y-2*Epsilon, world))
                {
                    //postPosition.z = Mathf.CeilToInt(Position.z-Size.z/2+z)+Size.z/2;
                    Velocity.z = 0;
                    collisionDirections |= (1<<(int)Direction.NegZ);
                    break;
                }
            }
        }
        else
        {
            //moving backward
            for (int z = 0; z < Mathf.CeilToInt(frameVelocity.z); z++)
            {
                if (Plane.CollidesWithWorldZ(Position + new Vector3(-Size.x/2+Epsilon,-Size.y/2+Epsilon, Size.z/2+z), Size.x-2*Epsilon, Size.y-2*Epsilon, world))
                {
                    postPosition.z = Mathf.FloorToInt(Position.z+Size.z/2+z)-Size.z/2;
                    Velocity.z = 0;
                    collisionDirections |= (1<<(int)Direction.PosZ);
                    break;
                }
            }
        }

        Position = postPosition;
    }
}