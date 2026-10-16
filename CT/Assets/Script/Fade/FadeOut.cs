
public class FadeOut : FadeBase
{
    protected override float StartAlpha { get { return 0.0f; } }
    protected override float EndAlpha { get { return 1.0f; } }
}