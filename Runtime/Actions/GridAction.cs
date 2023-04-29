
public interface GridAction
{
    public void OnStart();

    public void Update();

    public void Cancel();

    public void OnClick(bool pressedDown, bool released);

    public void OnRotateLeft();

    public void OnRotateRight();
}
