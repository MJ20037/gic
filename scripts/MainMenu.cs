using Godot;

public partial class MainMenu : Node2D
{
	public override void _Ready()
	{
		// Get the start button and connect its pressed signal
		Button startButton = GetNode<Button>("CanvasLayer/StartButton");
		startButton.Pressed += OnStartButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/LVL1.tscn");
	}
}
