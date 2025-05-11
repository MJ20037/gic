using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	// References
	private GridManager _grid;
	private GameManager _gameManager;
	private AnimatedSprite2D _anim;
	private Label _moveLabel;
	private TextureRect _pickaxeIcon;
	private CpuParticles2D _levelCompleteParticles;
	
	// State
	private Vector2I _currentTile;
	private int _movesRemaining = 0;
	private bool _isFrozen = false;
	private bool _isVisible = true;
	
	private AudioStreamPlayer2D _audioBushEnter;
	private AudioStreamPlayer2D _audioRockBreak;
	private AudioStreamPlayer2D _audioLevelComplete;
	private AudioStreamPlayer2D _audioDeath;

	
	// Public property for visibility
	public bool IsVisible => _isVisible;
	
	private bool _hasPickaxe = false;
	
	public void CollectPickaxe()
	{
		_hasPickaxe = true;
		_audioRockBreak?.Play();
		GD.Print("Player collected the pickaxe!");
		if (_pickaxeIcon != null)
			_pickaxeIcon.Visible = true;
	}
	
	public override void _Ready()
	{
		// Get references
		_grid = GetNode<GridManager>("/root/Main");
		_gameManager = GetNode<GameManager>("/root/Main/GameManager");
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_moveLabel = GetNodeOrNull<Label>("MoveLabel");
		_pickaxeIcon = GetNodeOrNull<TextureRect>("/root/Main/UI/PickaxeIcon");
		_levelCompleteParticles = GetNode<CpuParticles2D>("LevelCompleteParticles");

		
		_audioBushEnter = GetNode<AudioStreamPlayer2D>("AudioBushEnter");
		_audioRockBreak = GetNode<AudioStreamPlayer2D>("AudioRockBreak");
		_audioLevelComplete = GetNode<AudioStreamPlayer2D>("AudioLevelComplete");
		_audioDeath = GetNode<AudioStreamPlayer2D>("AudioDeath");

		
		// Initialize position
		_currentTile = _grid.WorldToMap(GlobalPosition);
		_moveLabel.SelfModulate = new Color(1, 1, 1, 1); 
		UpdateMoveLabel();
		UpdateVisibility();
		
	}
	
	public void SetMoves(int moves)
	{
		_movesRemaining = Mathf.Max(0, moves);
		UpdateMoveLabel();
	}
	
	private void UpdateMoveLabel()
	{
		if (_moveLabel != null)
		{
			_moveLabel.Text = _movesRemaining.ToString();
		}
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		// Ignore input if frozen, no moves left, or not a key press
		if (_isFrozen || _movesRemaining <= 0 || !(@event is InputEventKey keyEvent) || !keyEvent.Pressed)
			return;
			
		// Determine direction based on input
		Vector2I direction = Vector2I.Zero;
		if (Input.IsActionPressed("ui_up"))    direction = Vector2I.Up;
		else if (Input.IsActionPressed("ui_down"))  direction = Vector2I.Down;
		else if (Input.IsActionPressed("ui_left"))  direction = Vector2I.Left;
		else if (Input.IsActionPressed("ui_right")) direction = Vector2I.Right;
		else return;
		
		// Try to move in the given direction
		TryMove(direction);
	}
	
	private void TryMove(Vector2I direction)
{
	Vector2I targetTile = _currentTile + direction;

	// If it's a rock and we have the pickaxe, break it
	if (_grid.IsRock(targetTile) && _hasPickaxe)
	{
		int walkableTileId = 3; // <-- ID of your walkable tile
		_grid.Grid.SetCell(targetTile, walkableTileId, new Vector2I(0, 0));
		GD.Print($"Broke rock at {targetTile}");
		_audioRockBreak?.Play();
	}

	// Check if the target tile is walkable (pass pickaxe flag)
	if (_grid.IsWalkable(targetTile, _hasPickaxe))
	{
		_currentTile = targetTile;
		GlobalPosition = _grid.MapToWorld(_currentTile);

		_movesRemaining--;
		UpdateMoveLabel();
		UpdateVisibility();
		UpdateEnemiesVisibility();
		CheckEnemyCollision();
		if (_gameManager.CheckLevelComplete(_currentTile))
		{
			_gameManager.FreezeAllEnemies();
			_audioLevelComplete?.Play();
			_levelCompleteParticles.Emitting = true;
			return;
		}

		if (_movesRemaining <= 0)
			_gameManager.EndPlayerTurn();
	}
}

	
	private void CheckEnemyCollision()
{
	foreach (Enemy enemy in _gameManager.GetEnemies())
	{
		if (enemy.CurrentTile == _currentTile)
		{
			GD.Print("Player moved onto enemy tile! Game over.");

			//  Play enemy attack animation
			enemy.PlayAttack();

			// Freeze player and play death animation
			Freeze();

			// Restart level after short delay
			GetTree().CreateTimer(1.0f).Timeout += () => GetTree().ReloadCurrentScene();
			break;
		}
	}
}
	
	public void SetTile(Vector2I tile)
	{
		_currentTile = tile;
		GlobalPosition = _grid.MapToWorld(_currentTile);
		UpdateVisibility();
	}
	
	public void Freeze()
	{
		_isFrozen = true;
		_anim.Play("death");
		_audioDeath?.Play();
	}
	
	public void UpdateVisibility()
	{
		if (_grid == null || _gameManager == null) return;
		
		bool previousVisibility = _isVisible;
		
		// Default visibility - hidden in forest, visible elsewhere
		bool inForest = _grid.IsForest(_currentTile);
		
		if (inForest)
		{
			// In forest: check if any enemy is adjacent or same tile
			_isVisible = false; // Default to invisible in forest
			_audioBushEnter?.Play();
			
			foreach (Enemy enemy in _gameManager.GetEnemies())
			{
				if (enemy == null) continue;
				
				int dx = Mathf.Abs(enemy.CurrentTile.X - _currentTile.X);
				int dy = Mathf.Abs(enemy.CurrentTile.Y - _currentTile.Y);
				if ((dx + dy) <= 1) // Same or adjacent tile
				{
					_isVisible = true;
					break;
				}
			}
		}
		else
		{
			// Not in forest: always visible
			_isVisible = true;
		}
		
		// Log visibility changes
		if (_isVisible != previousVisibility)
		{
			if (_isVisible)
				GD.Print($"Player at {_currentTile} became visible");
			else
				GD.Print($"Player at {_currentTile} became invisible");
		}
		
		// Update visual elements - using Modulate to control visibility directly
		// This ensures the sprites are truly invisible
		if (_anim != null)
			_anim.Visible = _isVisible;
	}
	
	// Update enemies visibility when player moves
	private void UpdateEnemiesVisibility()
	{
		foreach (Enemy enemy in _gameManager.GetEnemies())
		{
			enemy.UpdateVisibility();
		}
	}
	
	// Called after all enemies move to update player visibility
	public void UpdateVisibilityWithEnemies(List<Enemy> enemies)
	{
		// Simply call the regular visibility update
		UpdateVisibility();
		
		// For debugging, print the visibility status
		GD.Print($"Player Visibility: {(_isVisible ? "Visible" : "Invisible")} at tile {_currentTile}, Forest: {_grid.IsForest(_currentTile)}");
	}
}
