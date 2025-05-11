using Godot;

public partial class CameraManager : Node
{
	[Export] public Camera2D Camera;
	[Export] public float FollowSpeed = 5f;

	private Node2D _target;
	private bool _justSwitchedTarget = false;

	public override void _Ready()
	{
		if (Camera == null)
			Camera = GetNode<Camera2D>("Camera2D");

		Camera.MakeCurrent();
	}

	public void Follow(Node2D target)
	{
		_target = target;
		_justSwitchedTarget = true; 
	}

	public override void _Process(double delta)
	{
		if (_target == null || Camera == null)
			return;

		if (_justSwitchedTarget)
		{
			Camera.GlobalPosition = _target.GlobalPosition;
			_justSwitchedTarget = false;
		}
		else
		{
			// Smooth follow
			Camera.GlobalPosition = Camera.GlobalPosition.Lerp(
				_target.GlobalPosition,
				FollowSpeed * (float)delta
			);
		}
	}
}
