using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Enemy : CharacterBody2D
{
	[Export] public int MaxMoves = 3;
	[Export] public int MinMoves = 1;
	[Export] public float MoveDuration = 0.4f;
	[Export] public bool MoveRandomlyWhenNoTarget = true;
	[Export] public CameraManager _cameraManager;
	
	private static HashSet<Vector2I> OccupiedTiles = new();
	
	private GridManager _grid;
	private Player _player;
	private AnimatedSprite2D _anim;
	private Label _moveLabel;
	private GameManager GameManager;
	
	private Vector2I _currentTile;
	private int _movesRemaining;
	private int _plannedMoves;
	private Vector2I _lastKnownPlayerPos = Vector2I.Zero;
	private bool _isMoving = false;
	private bool _isVisible = true;
	private bool _isInitialized = false;
	private bool _turnCompleted = true; 

	public Vector2I CurrentTile => _currentTile;
	public bool IsMoving => _isMoving;
	public bool IsVisible => _isVisible;
	public bool IsInitialized => _isInitialized;
	public bool TurnCompleted => _turnCompleted;
	
	private bool _isFrozen = false;

	public void Freeze()
	{
		_isFrozen = true;
	}

	public override void _Ready()
	{
		try
		{
			_grid = GetNode<GridManager>("/root/Main");
			
			_player = GetNode<Player>("/root/Main/Player");
			
			_anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			
			_moveLabel = GetNodeOrNull<Label>("MoveLabel");
			
			GameManager = GetNode<GameManager>("/root/Main/GameManager");
			
			_currentTile = _grid.WorldToMap(GlobalPosition);
			_plannedMoves = GD.RandRange(MinMoves, MaxMoves);
			_movesRemaining = _plannedMoves;
			
			OccupiedTiles.Add(_currentTile);
			
			UpdateMoveLabel();
			
			_isInitialized = true;
			
			GD.Print($"Enemy {Name} initialized successfully at {_currentTile}");
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Exception during Enemy initialization: {e.Message}\n{e.StackTrace}");
		}
	}

	public void SetStepsForNextTurn()
	{
		if (!_isInitialized|| GameManager.IsFrozen) return;
		
		if (_turnCompleted)
		{
			_plannedMoves = GD.RandRange(MinMoves, MaxMoves);
			_movesRemaining = _plannedMoves;
			_turnCompleted = false; 
			GD.Print($"Enemy {Name}: Set steps for next turn: {_movesRemaining}");
			UpdateMoveLabel();
		}
		else
		{
			GD.Print($"Enemy {Name}: Skipping SetStepsForNextTurn as previous turn not completed");
		}
	}

	public async void TakeTurn()
	{
		if (!_isInitialized || _movesRemaining <= 0 || _isMoving|| GameManager.IsFrozen)
		{
			_turnCompleted = true;
			GD.Print($"Enemy {Name}: Skipping turn - not initialized, no moves, or already moving");
			return;
		}
			
		_isMoving = true;
		_cameraManager?.Follow(this);
		
		try
		{
			OccupiedTiles.Remove(_currentTile);
			
			if (_player == null)
			{
				_player = GetNode<Player>("/root/Main/Player");
				if (_player == null)
				{
					GD.PrintErr($"Enemy {Name}: Player reference is null in TakeTurn");
					_isMoving = false;
					_turnCompleted = true;
					return;
				}
			}
			
			if (_grid == null)
			{
				GD.PrintErr($"Enemy {Name}: Grid reference is null in TakeTurn");
				_isMoving = false;
				_turnCompleted = true;
				return;
			}
			
			Vector2I playerTile = _grid.WorldToMap(_player.GlobalPosition);
			List<Vector2I> path = new();
			
			bool canSeePlayer = CanSeePlayer();
			
			GD.Print($"Enemy at {_currentTile} can see player: {canSeePlayer}");
			
			if (canSeePlayer)
			{
				_lastKnownPlayerPos = playerTile;
				path = _grid.FindPath(_currentTile, playerTile);
				GD.Print($"Enemy chasing player along path with {path.Count} steps");
			}
			else if (_lastKnownPlayerPos != Vector2I.Zero && _lastKnownPlayerPos != _currentTile)
			{
				path = _grid.FindPath(_currentTile, _lastKnownPlayerPos);
				GD.Print($"Enemy moving to last known player position {_lastKnownPlayerPos}");
				
				if (path.Count == 0 || path[0] == _lastKnownPlayerPos)
				{
					GD.Print("Enemy reached last known position, will start moving randomly");
					_lastKnownPlayerPos = Vector2I.Zero;
					path.Clear();
				}
			}
			
			if ((path.Count == 0 || _lastKnownPlayerPos == Vector2I.Zero) && MoveRandomlyWhenNoTarget)
			{
				path = GetRandomPath(_movesRemaining);
				GD.Print($"Enemy moving randomly along path with {path.Count} steps");
			}
			
			int stepsToMove = Mathf.Min(_movesRemaining, path.Count);
			
			for (int i = 0; i < stepsToMove; i++)
			{
				Vector2I nextTile = path[i];
				
				if (!_grid.IsWalkable(nextTile) || OccupiedTiles.Contains(nextTile))
					break;
					
				await MoveToTile(nextTile);
				
				_currentTile = nextTile;
				_movesRemaining--;
				UpdateMoveLabel();
				UpdateVisibility();
				
				playerTile = _grid.WorldToMap(_player.GlobalPosition);
				if (_grid.CanSee(_currentTile, playerTile))
				{
					_lastKnownPlayerPos = playerTile;
				}
				
				if (_currentTile == playerTile)
				{
					GD.Print("Enemy caught player!");
					if (_anim != null)
						_anim.Play("attack");
					_player.Freeze();
					await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
					GetTree().ReloadCurrentScene();
					return;
				}
				
				await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			}
			
			OccupiedTiles.Add(_currentTile);
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Exception during Enemy.TakeTurn: {e.Message}\n{e.StackTrace}");
		}
		finally
		{
			_isMoving = false;
			_turnCompleted = true; 
			GD.Print($"Enemy {Name}: Turn completed with {_movesRemaining} moves remaining");
		}
	}

	private async Task MoveToTile(Vector2I targetTile)
	{
		if (_grid == null) return;
		
		Vector2 targetPos = _grid.MapToWorld(targetTile);
		
		if (_anim != null)
			_anim.Play("run");
		
		var tween = GetTree().CreateTween();
		tween.TweenProperty(this, "global_position", targetPos, MoveDuration);
		await ToSignal(tween, "finished");
		
		if (_anim != null)
			_anim.Play("idle");
	}

	private void UpdateMoveLabel()
	{
		if (_moveLabel == null) return;

		int display = (!_turnCompleted && _isMoving) 
						? Mathf.Max(0, _movesRemaining) 
						: _plannedMoves;

		_moveLabel.Text    = display.ToString();
		_moveLabel.Visible = _isVisible;
	}

	public void OnPlayerCollided()
	{
		if (_anim != null)
			_anim.Play("attack");
	}

	public void UpdateVisibility()
	{
		if (!_isInitialized)
		{
			GD.Print($"Enemy {Name}: UpdateVisibility called before initialization");
			return;
		}
		
		try
		{
			if (_grid == null)
			{
				GD.PrintErr($"Enemy {Name}: Grid reference is null in UpdateVisibility");
				return;
			}
			
			if (_player == null)
			{
				GD.PrintErr($"Enemy {Name}: Player reference is null in UpdateVisibility");
				return;
			}
			
			bool previousVisibility = _isVisible;
			
			// Check if enemy is in forest
			bool inForest = _grid.IsForest(_currentTile);
			
			if (inForest)
			{
				// In forest: only visible if player is adjacent or same tile
				Vector2I playerTile = _grid.WorldToMap(_player.GlobalPosition);
				int dx = Mathf.Abs(playerTile.X - _currentTile.X);
				int dy = Mathf.Abs(playerTile.Y - _currentTile.Y);
				bool isAdjacentToPlayer = (dx + dy) <= 1; // Same or adjacent tile
				
				_isVisible = isAdjacentToPlayer;
			}
			else
			{
				// Not in forest: always visible
				_isVisible = true;
			}
			
			// Log visibility state
			if (_isVisible != previousVisibility)
			{
				if (_isVisible)
					GD.Print($"Enemy at {_currentTile} became visible");
				else
					GD.Print($"Enemy at {_currentTile} became invisible");
			}
			
			// Update visual elements - THIS IS KEY - directly modify Modulate property
			// rather than just the Visible property
			Modulate = _isVisible ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
			
			if (_moveLabel != null)
			{
				_moveLabel.Visible = _isVisible;
			}
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Exception during Enemy.UpdateVisibility: {e.Message}\n{e.StackTrace}");
		}
	}
	
	public void PlayAttack()
	{
		if (_anim != null)
			_anim.Play("attack"); 
	}

	private List<Vector2I> GetRandomPath(int steps)
	{
		List<Vector2I> path = new();
		Vector2I current = _currentTile;
		
		// Try to move up to 'steps' times
		for (int i = 0; i < steps; i++)
		{
			// Get all possible directions in random order
			List<Vector2I> directions = new() { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };
			Shuffle(directions);
			
			bool foundMove = false;
			foreach (Vector2I dir in directions)
			{
				Vector2I nextPos = current + dir;
				
				// Check if the position is valid
				if (_grid != null && _grid.IsWalkable(nextPos) && !OccupiedTiles.Contains(nextPos))
				{
					path.Add(nextPos);
					current = nextPos;
					foundMove = true;
					break;
				}
			}
			
			// If no valid move found, stop planning path
			if (!foundMove)
				break;
		}
		
		return path;
	}

	private void Shuffle<T>(List<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = (int)(GD.Randi() % (uint)(n + 1));
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}
	
	public bool CanSeePlayer()
	{
		// Safety check
		if (_player == null || _grid == null)
		{
			GD.PrintErr($"Enemy {Name}: Missing references in CanSeePlayer");
			return false;
		}
	
		// Get player position
		Vector2I playerTile = _grid.WorldToMap(_player.GlobalPosition);
		
		// For adjacency check
		int dx = Mathf.Abs(playerTile.X - _currentTile.X);
		int dy = Mathf.Abs(playerTile.Y - _currentTile.Y);
		bool isAdjacent = (dx + dy) <= 1; // Same or adjacent tile
		
		// Player is only visible if:
		// 1. Player is not in forest, OR
		// 2. Player is in forest BUT we're adjacent or in the same tile
		return !_grid.IsForest(playerTile) || isAdjacent;
	}
	
	public static void ClearOccupiedTiles()
	{
		OccupiedTiles.Clear();
	}
	
	public static void RegisterOccupiedTile(Vector2I tile)
	{
		OccupiedTiles.Add(tile);
	}
}
