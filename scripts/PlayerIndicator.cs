using Godot;

public partial class PlayerIndicator : CanvasLayer
{
	[Export] public NodePath PlayerPath;
	[Export] public NodePath CameraPath;

	private Node2D _player;
	private Camera2D _camera;
	private Sprite2D _indicator;

	public override void _Ready()
	{
		_player = GetNode<Node2D>(PlayerPath);
		_camera = GetNode<Camera2D>(CameraPath);
		_indicator = GetNode<Sprite2D>("IndicatorSprite");
	}

	public override void _Process(double delta)
	{
		if (_player != null && _camera != null && _indicator != null)
		{
			// Convert world position (global) to screen position (viewport)
			Vector2 screenPos = _camera.GetScreenCenterPosition() + (_player.GlobalPosition - _camera.GlobalPosition);
			_indicator.Position = screenPos + new Vector2(0, -10);
		}
	}
}
