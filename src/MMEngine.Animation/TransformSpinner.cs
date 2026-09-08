using MMEngine.Core;
using MMEngine.Scene;

namespace MMEngine.Animation;

public sealed class TransformSpinner : MMBehaviour
{
    public float RadiansPerSecond { get; set; } = 1f;

    protected override void Update(in FrameTime time)
        => Transform.RotateY(RadiansPerSecond * (float)time.DeltaSeconds);
}
