using Godot;

public partial class MiniMapToggle : CanvasLayer
{
	[Export] public NodePath MiniMapLayer;
	private CanvasLayer _minimapLayer;

	public override void _Ready()
	{
		_minimapLayer = GetNode<CanvasLayer>(MiniMapLayer);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.M)
		{
			_minimapLayer.Visible = !_minimapLayer.Visible;
		}
	}
}
