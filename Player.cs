using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private AnimatedSprite2D animatedSprite;

	public override void _Ready()
{
	base._Ready();
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	// ... any other initialization code ...
}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}
		
		// Animation state
		if (!IsOnFloor())
		{
			animatedSprite.Play("jump");
		}
		else if (IsOnFloor() && velocity.Length() == 0)
		{
			animatedSprite.Play("idle");
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
