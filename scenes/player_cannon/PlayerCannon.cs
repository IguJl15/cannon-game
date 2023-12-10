using Godot;
using System;

public partial class PlayerCannon : CharacterBody3D
{
	public Camera3D Camera;
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public float MouseSensitivity = 1f / 1000f;
	public float TwistInput = 0f;
	public float PitchInput = 0f;

	public Node3D CannonPitch;
	public Node3D TwistNode;
	public Node3D PitchNode;

	public bool isAiming = false;
	public float upViewLimit = 0.3f;
	public float downViewLimit = -0.5f;

	public PackedScene BallScene;
	public Node3D BallSpawnPoint;
	public AudioStreamPlayer3D shootAudioPlayer;
	public CpuParticles3D shootParticles;

	public override void _Ready()
	{
		base._Ready();

		Input.MouseMode = Input.MouseModeEnum.Captured;

		TwistNode = GetNode<Node3D>("TwistPivot");
		CannonPitch = GetNode<Node3D>("CannonBarrel");
		PitchNode = GetNode<Node3D>("TwistPivot/PitchPivot");
		Camera = GetNode<Camera3D>("TwistPivot/PitchPivot/Camera3D");

		BallSpawnPoint = GetNode<Node3D>("CannonBarrel/CannonBallSpawnPoint");
		BallScene = GD.Load<PackedScene>("res://scenes/player_cannon/cannon_ball/CannonBall.tscn");
		shootAudioPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		shootParticles = GetNode<CpuParticles3D>("CannonBarrel/CannonBallSpawnPoint/FireParticles");
	}

	public override void _PhysicsProcess(double delta)
	{

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			Input.MouseMode =
				Input.MouseMode == Input.MouseModeEnum.Captured
					? Input.MouseModeEnum.Visible
					: Input.MouseModeEnum.Captured;
		}

		RotateY(TwistInput);

		PitchNode.RotateX(PitchInput);
		var clampedXRotation = new Vector3((float) Math.Clamp(PitchNode.Rotation.X, downViewLimit, upViewLimit), PitchNode.Rotation.Y, PitchNode.Rotation.Z);
		PitchNode.Rotation = clampedXRotation;
		CannonPitch.Rotation = clampedXRotation;

		PitchInput = 0.0f;
		TwistInput = 0.0f;


		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		Vector2 inputDir = Input.GetVector("rotate_left", "rotate_right", "move_forward", "move_backward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{

			if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

			TwistInput = -(@event as InputEventMouseMotion).Relative.X * MouseSensitivity;
			PitchInput = -(@event as InputEventMouseMotion).Relative.Y * MouseSensitivity;
		}

		if(@event is InputEventMouseButton && @event.IsAction("aim")) {
			if(Input.IsActionPressed("aim")) {
				isAiming = true;

				Camera.Position = new Vector3(0.5f, 0.25f, 0.35f);
				Camera.Fov = 50;
			} else {
				isAiming = false;

				Camera.Position = new Vector3(0.5f, 0f, 3f);
				Camera.Fov = 75f;
			}
		}

		if (@event is InputEventMouseButton && (@event as InputEventMouseButton).IsActionPressed("shoot")) {
			Shoot();
		}
	}

	private void Shoot() {
		var ball = BallScene.Instantiate<RigidBody3D>();

		ball.GlobalPosition = BallSpawnPoint.GlobalPosition;
		var direction = (BallSpawnPoint.GlobalPosition - CannonPitch.GlobalPosition).Normalized();
		var force = 36;

		ball.ApplyCentralImpulse(direction * force);

		shootAudioPlayer.Play();
		shootParticles.Restart();
		GetTree().Root.AddChild(ball);
	}
	// while aiming:
	// Camera - Fov: 55º (era 75), xyz: 0.6, -1, 3 (era 0, 0, 3), near 0.002 (era )
}
