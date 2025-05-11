using Godot;
using System;

public partial class MovingCircle : Node2D
{
	public float Radius = 50f;
	private Vector2 velocity;
	private Color circleColor = new Color(0.482f, 0.247f, 0.604f); 
	private Vector2 circlePosition;

	private bool initialized = false;

	public override void _Process(double delta)
	{
		if (!initialized)
		{
			Initialize();
			initialized = true;
		}

		circlePosition += velocity * (float)delta;

		// Wrap around the screen
		Vector2 screenSize = GetViewport().GetVisibleRect().Size;
		if (circlePosition.X < -Radius) circlePosition.X = screenSize.X + Radius;
		if (circlePosition.X > screenSize.X + Radius) circlePosition.X = -Radius;
		if (circlePosition.Y < -Radius) circlePosition.Y = screenSize.Y + Radius;
		if (circlePosition.Y > screenSize.Y + Radius) circlePosition.Y = -Radius;

		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawCircle(circlePosition, Radius, circleColor);
	}

	private void Initialize()
	{
		Random rand = new Random();

		// Set random radius
		Radius = (float)(rand.NextDouble() * 40 + 20);

		// Set random initial velocity
		velocity = new Vector2(
			(float)(rand.NextDouble() * 2 - 1),
			(float)(rand.NextDouble() * 2 - 1)
		).Normalized() * 20f;

		// Get screen size properly
		Vector2 screenSize = GetViewport().GetVisibleRect().Size;

		// Random starting position across the full screen
		circlePosition = new Vector2(
			(float)(rand.NextDouble() * screenSize.X),
			(float)(rand.NextDouble() * screenSize.Y)
		);
	}
}
