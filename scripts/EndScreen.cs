using Godot;

public partial class EndScreen : Node2D
{
	public override void _Ready()
	{
		Button startButton = GetNode<Button>("CanvasLayer/MenuButton");
		startButton.Pressed += OnStartButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}
}
