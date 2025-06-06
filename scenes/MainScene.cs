using Godot;
using System;
using System.Collections.Generic;

public partial class MainScene : Node
{
    [Signal]
    public delegate void GameStateChangedEventHandler(bool running);
    private PackedScene stumpScene = GD.Load<PackedScene>("res://scenes/tree_stump.tscn");
    private PackedScene bushScene = GD.Load<PackedScene>("res://scenes/bush.tscn");
    private PackedScene birdScene = GD.Load<PackedScene>("res://scenes/bird.tscn");
    private List<PackedScene> obstacleTypes;
    private List<Node2D> obstacles = new();
    private int[] birdHeights = new int[] {315, 325};
    private Vector2I DINO_START_POS = new Vector2I(150, 485);
    private Vector2I CAM_START_POS = new Vector2I(576, 324);
    private int difficulty;
    private const int MAX_DIFFICULTY = 2;
    private int score;
    private const int SCORE_MODIFIER = 10;
    private int highScore;
    private float speed;
    private const float START_SPEED = 10.0f;
    private const int MAX_SPEED = 25;
    private const int SPEED_MODIFIER = 5000;
    private Vector2I screenSize;
    private int groundHeight;
    private bool gameRunning = false;
    private Node2D lastObs;

    public override void _Ready()
    {
        obstacleTypes = new List<PackedScene> { stumpScene, bushScene};
        screenSize = GetWindow().Size;
        groundHeight = GetNode<Sprite2D>("ground/Sprite2D").Texture.GetHeight()/5;
        
        var restartButton = GetNode<Button>("GameOver/Button");
        restartButton.SetProcessMode(ProcessModeEnum.Always);
        restartButton.Connect("pressed", new Callable(this, nameof(NewGame)));

        NewGame();
    } 
    private void NewGame()
    {
        score = 0;
        ShowScore();
        gameRunning = false;
        GetTree().Paused = false;
        difficulty = 0;
        foreach (var obs in obstacles)
            obs.QueueFree();
        obstacles.Clear();
        var dino = GetNode<CharacterBody2D>("Player");
        dino.Position = DINO_START_POS;
        dino.Velocity = Vector2I.Zero;
        GetNode<Camera2D>("Camera2D").Position = CAM_START_POS;
        GetNode<Node2D>("ground").Position = Vector2I.Zero;
        GetNode<Label>("HUD/Play").Show();
        GetNode<CanvasLayer>("GameOver").Hide();
    }

    public override void _Process(double delta)
    {
        if (gameRunning)
        {
            speed = START_SPEED + score / (float)SPEED_MODIFIER;
            if (speed > MAX_SPEED)
                speed = MAX_SPEED;
            AdjustDifficulty();
            GenerateObstacles();
            var dino = GetNode<CharacterBody2D>("Player");
            var camera = GetNode<Camera2D>("Camera2D");
            dino.Position += new Vector2((float)speed, 0);
            camera.Position += new Vector2((float)speed, 0);
            score += (int)speed;
            ShowScore();
            if (camera.Position.X - GetNode<Node2D>("ground").Position.X > screenSize.X * 1.5)
                GetNode<Node2D>("ground").Position += new Vector2(screenSize.X, 0);
            foreach (var obs in new List<Node2D>(obstacles))
            {
                if (obs.Position.X < (camera.Position.X - screenSize.X))
                    RemoveObstacle(obs);
            }
        }
        else if (Input.IsActionPressed("ui_accept"))
        {
            gameRunning = true;
            GetNode<Label>("HUD/Play").Hide();
            EmitSignal("GameStateChanged", true);
        }
    }

    private void GenerateObstacles()
    {
        if (obstacles.Count == 0 && obstacles.Count < 2 || lastObs.Position.X < score + GD.RandRange(300, 500))
        {
            var obsType = obstacleTypes[(int)(GD.Randi() % (uint)obstacleTypes.Count)];
            Node2D obs = null;
            int maxObs = Mathf.Min(2, difficulty + 1);

            for (int i = 0; i < GD.Randi() % (maxObs + 1); i++)
            {
                obs = obsType.Instantiate<Node2D>();
                var sprite = obs.GetNode<Sprite2D>("Sprite2D");
                int obsHeight = sprite.Texture.GetHeight();
                Vector2 obsScale = sprite.Scale;
                int obsX = screenSize.X + score + 100 + (i * 100);
                int obsY = screenSize.Y - groundHeight - (int)(obsHeight * obsScale.Y / 2) + 5;   
                lastObs = obs;
                AddObstacle(obs, obsX, obsY);
            }
            if (difficulty == MAX_DIFFICULTY && GD.Randi() % 2 == 0)
            {
                obs = birdScene.Instantiate<Node2D>();
                int obsX = screenSize.X + score + 100;
                int obsY = birdHeights[GD.Randi() % birdHeights.Length];
                AddObstacle(obs, obsX, obsY);
            }
        }
    }
    private void AddObstacle(Node2D obs, int x, int y)
    {
        obs.Position = new Vector2I(x, y);
        obs.Connect("body_entered", new Callable(this, nameof(HitObstacle)));
        AddChild(obs);
        obstacles.Add(obs);
    }
    private void RemoveObstacle(Node2D obs)
    {
        obs.QueueFree();
        obstacles.Remove(obs);
    }
    private void HitObstacle(Node body)
    {
        if (body.Name == "Player")
            GameOver();
    }
    private void ShowScore()
    {
        GetNode<Label>("HUD/Score").Text = $"SCORE: {score / SCORE_MODIFIER}";
    }

    private void CheckHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            GetNode<Label>("HUD/HighScore").Text = $"HIGH SCORE: {highScore / SCORE_MODIFIER}";
        }
    }
    private void AdjustDifficulty()
    {
        difficulty = score / SPEED_MODIFIER;
        if (difficulty > MAX_DIFFICULTY)
            difficulty = MAX_DIFFICULTY;
    }
    private void GameOver()
    {
        CheckHighScore();
        GetTree().Paused = true;
        gameRunning = false;
        GetNode<CanvasLayer>("GameOver").Show();
        EmitSignal("GameStateChanged", false);
    }
}
