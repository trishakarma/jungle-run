using Godot;
using System;

public partial class Player : CharacterBody2D
{
    private const int GRAVITY = 4100;
    private const int JUMP_SPEED = -1800;

    private bool _gameRunning = false;

    public override void _Ready()
    {
        // Connect to the GameStateChanged signal from the parent node (MainScene)
        GetParent().Connect("GameStateChanged", new Callable(this, nameof(OnGameStateChanged)));
    }

    private void OnGameStateChanged(bool running)
    {
        _gameRunning = running;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply gravity
        Velocity = new Vector2(Velocity.X, Velocity.Y + (float)(GRAVITY * delta));

        if (IsOnFloor())
        {
            if (!_gameRunning)
            {
                GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("idle");
            }
            else
            {
                GetNode<CollisionShape2D>("runCollision").Disabled = false;

                if (Input.IsActionPressed("ui_accept"))
                {
                    Velocity = new Vector2(Velocity.X, JUMP_SPEED);
                }
                else if (Input.IsActionPressed("ui_down"))
                {
                    GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("duck");
                    GetNode<CollisionShape2D>("runCollision").Disabled = true;
                }
                else
                {
                    GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("run");
                }
            }
        }
        else
        {
            GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("jump");
        }

        MoveAndSlide();
    }
}
