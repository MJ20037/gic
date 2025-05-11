using Godot;

public partial class Pickaxe : Area2D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	private void OnBodyEntered(Node body)
	{
		if (body is Player player)
		{
			player.CollectPickaxe();
			QueueFree(); // remove the pickaxe from the scene
		}
	}
}
