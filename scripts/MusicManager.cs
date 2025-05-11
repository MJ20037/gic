using Godot;
using System.Collections.Generic;

public partial class MusicManager : Node
{
	[Export]
	public AudioStream[] Songs = new AudioStream[0];

	private int _currentSongIndex = 0;
	private AudioStreamPlayer2D _player;

	public override void _Ready()
	{
		_player = GetNode<AudioStreamPlayer2D>("Player");

		if (Songs.Length > 0)
		{
			_player.Stream = Songs[_currentSongIndex];
			_player.Play();
		}

		_player.Finished += OnSongFinished;
	}

	private void OnSongFinished()
	{
		_currentSongIndex = (_currentSongIndex + 1) % Songs.Length;
		_player.Stream = Songs[_currentSongIndex];
		_player.Play();
	}
}
