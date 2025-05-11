using Godot;
using System;

public partial class Background : CanvasLayer
{
	[Export]
	public PackedScene MovingCircleScene;

	public override void _Ready()
	{
		for (int i = 0; i < 20; i++)
		{
			Node2D circle = MovingCircleScene.Instantiate<Node2D>();
			AddChild(circle);
		}
	}
}
