using Godot;
using System.Collections.Generic;
public partial class GameManager : Node
{
	[Export] public Player Player;
	[Export] public NodePath enemiesContainer;
	[Export] public GridManager GridManager;
	[Export] public Marker2D StartPoint;
	[Export] public Marker2D EndPoint;
	[Export] public DiceUI DiceUI;
	[Export] public CameraManager CameraManager;
	[Export] public PackedScene NextLevel;
	
	private List<Enemy> _enemies = new();
	private Vector2I _startTile;
	private Vector2I _endTile;
	private bool _isProcessingTurn = false;
	
	// Singleton instance
	public static GameManager Instance { get; private set; }
	
	public override void _Ready()
	{
		Instance = this;
		
		// Validate required references before proceeding
		if (Player == null || GridManager == null || StartPoint == null || EndPoint == null)
		{
			GD.PrintErr("GameManager: Missing required node references!");
			return;
		}
		
		// Get map positions for start and end points
		_startTile = GridManager.WorldToMap(StartPoint.GlobalPosition);
		_endTile = GridManager.WorldToMap(EndPoint.GlobalPosition);
		
		// Place player at start position
		Player.SetTile(_startTile);
		
		// Clear any previous enemy positions
		Enemy.ClearOccupiedTiles();
		
		// Get all enemies from the container
		_enemies.Clear();
		var container = GetNode<Node2D>(enemiesContainer);
		if (container == null)
		{
			GD.PrintErr("GameManager: Enemies container not found!");
			return;
		}
		
		foreach (var node in container.GetChildren())
		{
			if (node is Enemy enemy)
			{
				_enemies.Add(enemy);
			}
			else
			{
				GD.PushError($"Invalid node in enemies container: {node.Name} is not an Enemy.");
			}
		}
		
		// Use CallDeferred to ensure all nodes have completed their _Ready() methods
		// before updating visibility and starting the game
		CallDeferred(nameof(InitializeGameplay));
		CameraManager?.Follow(Player);
	}
	
	private void InitializeGameplay()
	{
		// Validate DiceUI before using it
		if (DiceUI == null)
		{
			GD.PrintErr("GameManager: DiceUI reference is null!");
			return;
		}
		
		// Initialize enemy steps for first turn
		foreach (var enemy in _enemies)
		{
			if (enemy == null) continue;
			enemy.SetStepsForNextTurn();
		}
		
		// Make sure player and enemy visibility is updated properly
		UpdateAllVisibility();
		
		// Start the first player turn
		StartPlayerTurn();
	}
	
	private void StartPlayerTurn()
	{
		// give each enemy their next‐turn moves immediately
		foreach (var enemy in _enemies)
			enemy.SetStepsForNextTurn();
			
		CameraManager?.Follow(Player);

		DiceUI.StartPlayerTurn();
		CheckPlayerEnemyCollision();
	}
	
	private void CheckPlayerEnemyCollision()
	{
		if (Player == null) return;
		
		Vector2I playerTile = GridManager.WorldToMap(Player.GlobalPosition);
		
		foreach (Enemy enemy in _enemies)
		{
			if (enemy == null) continue;
			
			if (enemy.CurrentTile == playerTile)
			{
				GD.Print("Game over: Player and enemy on same tile!");
				Player.Freeze();
				GetTree().CreateTimer(1.0f).Timeout += () => GetTree().ReloadCurrentScene();
				break;
			}
		}
	}
	
	public async void EndPlayerTurn()
{
	if (_isProcessingTurn)
		return;
	_isProcessingTurn = true;

	// 1) Prepare each enemy for their next turn
	foreach (var enemy in _enemies)
	{
		if (enemy == null) continue;
		enemy.SetStepsForNextTurn();
	}

	// 2) Execute each enemy's turn one‑at‑a‑time
	foreach (var enemy in _enemies)
	{
		if (enemy == null || enemy.IsMoving)
			continue;

		// make the camera follow this enemy while it moves
		CameraManager?.Follow(enemy);

		// start its turn
		enemy.TakeTurn();

		// wait until that enemy really finishes (their TurnCompleted flag flips)
		while (!enemy.TurnCompleted)
			await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
	}

	// 3) update visibility, then return camera to player
	UpdateAllVisibility();
	CameraManager?.Follow(Player);

	_isProcessingTurn = false;
	StartPlayerTurn();
}

	
	// Update visibility for all game entities
	private void UpdateAllVisibility()
	{
		// Check if player exists
		if (Player == null)
		{
			GD.PrintErr("GameManager: Cannot update visibility - Player is null");
			return;
		}
		
		// First update player's visibility
		Player.UpdateVisibilityWithEnemies(_enemies);
		
		// Then update each enemy's visibility
		foreach (Enemy enemy in _enemies)
		{
			if (enemy == null) continue;
			enemy.UpdateVisibility();
		}
		
		// Debug output - print visibility status of all entities
		GD.Print("--- Visibility Status ---");
		Vector2I playerTile = GridManager.WorldToMap(Player.GlobalPosition);
		GD.Print($"Player at {playerTile}: {(Player.IsVisible ? "Visible" : "Invisible")}, Forest: {GridManager.IsForest(playerTile)}");
		
		foreach (Enemy enemy in _enemies)
		{
			if (enemy == null) continue;
			GD.Print($"Enemy at {enemy.CurrentTile}: {(enemy.IsVisible ? "Visible" : "Invisible")}, Forest: {GridManager.IsForest(enemy.CurrentTile)}");
		}
	}
	
	public bool CheckLevelComplete(Vector2I playerTile)
	{
		if (playerTile != _endTile)
			return false;

		GD.Print("Level Complete! Loading next level…");
		

		if (NextLevel != null)
		{
			// Immediately switch to the packed scene you assigned
			
			GetTree().CreateTimer(1.5f).Timeout += () =>
			{
				GetTree().ChangeSceneToPacked(NextLevel);
			};
		}
		else
		{
			GD.PrintErr("GameManager: NextLevel is not assigned!");
		}
		return true;
	}

	public bool IsFrozen { get; private set; } = false;
	
	public void FreezeAllEnemies()
	{
		IsFrozen = true;
		foreach (Enemy enemy in GetEnemies())
		{
			enemy.Freeze();
		}
	}
	
	// Access to enemies for other scripts
	public List<Enemy> GetEnemies()
	{
		return _enemies;
	}
}
