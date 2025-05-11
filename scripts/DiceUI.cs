using Godot;
using System.Threading.Tasks;

public partial class DiceUI : CanvasLayer
{
	[Export] public NodePath PlayerPath;
	[Export] public Button RollButton;
	[Export] public Label ResultLabel;

	private Player _player;
	private int _rollSessionId = 0;

	public override void _Ready()
	{
		_player = GetNode<Player>(PlayerPath);
		RollButton.Pressed += OnRollPressed;
	}

	private void OnRollPressed()
	{
		_ = HandleRollAsync();
	}

	private async Task HandleRollAsync()
	{
		int roll = GD.RandRange(1, 6);
		ResultLabel.Text = $"Result: {roll}";
		_player.SetMoves(roll);
		RollButton.Disabled = true;

		_rollSessionId++;
		int currentSession = _rollSessionId;

		await ToSignal(GetTree().CreateTimer(1f), "timeout");

		if (currentSession == _rollSessionId)
		{
			Hide();
		}
	}

	public void StartPlayerTurn()
	{
		ResultLabel.Text = "Roll the dice!";
		RollButton.Disabled = false;
		Show();
	}
}
