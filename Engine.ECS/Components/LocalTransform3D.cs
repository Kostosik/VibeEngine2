using Engine.Core.Math;
using Engine.Transform;

namespace Engine.ECS.Components;

public struct LocalTransform3D
{
    public LocalTransform3D(Transform3D value)
    {
        Value = value;
    }

    public LocalTransform3D()
    {
        Value = Transform3D.Identity;
    }

    public Transform3D Value { get; set; }

    public Vector3 Position
    {
        readonly get => Value.Position;
        set
        {
            var transform = Value;
            transform.Position = value;
            Value = transform;
        }
    }

    public Quaternion Rotation
    {
        readonly get => Value.Rotation;
        set
        {
            var transform = Value;
            transform.Rotation = value;
            Value = transform;
        }
    }

    public Vector3 Scale
    {
        readonly get => Value.Scale;
        set
        {
            var transform = Value;
            transform.Scale = value;
            Value = transform;
        }
    }
}