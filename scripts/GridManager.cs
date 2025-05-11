using Godot;
using System.Collections.Generic;

public partial class GridManager : Node2D
{
	[Export] public TileMapLayer Grid;
	[Export] public NodePath SubViewPortPath;

	// four cardinal directions
	private static readonly Vector2I[] DIRS = {
		Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
	};
	
	public bool IsRock(Vector2I pos)
	{
		var tileData = Grid.GetCellTileData(pos);
		if (tileData == null) return false;
		return tileData.HasCustomData("is_rock") && (bool)tileData.GetCustomData("is_rock");
	}
	
	public override void _Ready()
	{
		// Reference to your SubViewport node
		var minimapViewport = GetNode<SubViewport>(SubViewPortPath);

		// Assign the main viewport's World2D to the SubViewport
		minimapViewport.World2D = GetViewport().World2D;
	}

	/// <summary>
	/// Safely fetches a bool custom‑data value from the tile at pos.
	/// </summary>
	private bool GetBoolData(Vector2I pos, string key)
	{
		// fetch the TileData at these coords
		TileData data = Grid.GetCellTileData(pos);
		if (data == null)
			return false;

		if (!data.HasCustomData(key))
			return false;

		return (bool)data.GetCustomData(key);
	}

	public bool IsWalkable(Vector2I pos, bool hasPickaxe = false)
	{
		
		var tileData = Grid.GetCellTileData(pos);
		if (tileData == null)
		{
			GD.Print($"IsWalkable: No tile at {pos}");
			return false;
		}
		bool walkable = GetBoolData(pos, "is_walkable");
		bool isRock = IsRock(pos);

		// If it's a rock tile and the player doesn't have a pickaxe, it's not walkable
		if (isRock && !hasPickaxe)
		{
			GD.Print($"Tile at {pos} is a rock and not walkable without a pickaxe.");
			return false;
		}

		if (!walkable && !isRock)
		{
			GD.Print($"Tile at {pos} is not walkable.");
		}

		return true;
	}
	
	public bool IsForest(Vector2I pos) => GetBoolData(pos, "is_forest");
	public Vector2 TileSize => Grid.TileSet.GetTileSize();

	/// <summary>
	/// Determines if the observer can see the target based on forest rules
	/// </summary>
	public bool CanSee(Vector2I observerTile, Vector2I targetTile)
	{
		// Always visible if it's the same tile
		if (observerTile == targetTile)
			return true;

		// Calculate adjacency
		int dx = Mathf.Abs(observerTile.X - targetTile.X);
		int dy = Mathf.Abs(observerTile.Y - targetTile.Y);
		bool isAdjacent = (dx + dy) == 1; // Directly adjacent (not including diagonals)

		// If target is in forest, you can only see it if adjacent
		if (IsForest(targetTile))
			return isAdjacent;
			
		// If target is not in forest, it's always visible
		return true;
	}

	/// <summary>
	/// Simple BFS pathfinder on our grid of walkable tiles.
	/// </summary>
	public List<Vector2I> FindPath(Vector2I start, Vector2I goal)
	{
		if (!IsWalkable(goal))
		{
			GD.Print($"FindPath: Goal tile {goal} is not walkable");
			return new List<Vector2I>();
		}

		var visited = new HashSet<Vector2I> { start };
		var queue   = new Queue<Vector2I>();
		var parent  = new Dictionary<Vector2I, Vector2I>();
		queue.Enqueue(start);

		while (queue.Count > 0)
		{
			var u = queue.Dequeue();
			if (u == goal)
				break;

			foreach (var d in DIRS)
			{
				var v = u + d;
				if (visited.Contains(v) || !IsWalkable(v))
					continue;

				visited.Add(v);
				parent[v] = u;
				queue.Enqueue(v);
			}
		}

		var path = new List<Vector2I>();
		if (!parent.ContainsKey(goal))
		{
			GD.Print($"FindPath: No path found from {start} to {goal}");
			return path;  // no path
		}

		for (var cur = goal; cur != start; cur = parent[cur])
			path.Add(cur);
		path.Reverse();
		
		GD.Print($"FindPath: Found path with {path.Count} steps from {start} to {goal}");
		return path;
	}

	public Vector2I WorldToMap(Vector2 w) => Grid.LocalToMap(w);
	public Vector2   MapToWorld(Vector2I m) => Grid.MapToLocal(m);
}
