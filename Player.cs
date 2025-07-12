using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed = 300.0f;
    [Export]
    public float JumpVelocity = -650.0f;
    [Export]
    public float GroundAcceleration = 1200.0f;
    [Export]
    public float GroundFriction = 1200.0f;
    [Export]
    public float AirAcceleration = 800.0f;
    [Export]
    public float AirFriction = 60.0f;
    [Export]
    public float CoyoteTime = 0.15f;
   
    // Get gravity from project settings
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    
    public Vector2 AimDirection { get; private set; } = Vector2.Right;
    
    private bool dropThroughPressed = false;
    private float coyoteTimer = 0.0f;
    private bool wasOnFloor = false;
    
    // Spell casting state
    private bool hasSpellPrepared = false;
    private SpellData preparedSpell = null;
    
    // Aim indicator
    private AimIndicator aimIndicator;
    
    // Element buffer reference
    private ElementBufferUI elementUI;
    private SpellSystem spellSystem;
   
    public override void _Ready()
    {
        GD.Print("Player ready!");
        
        // Create and add aim indicator as a child
        var aimIndicatorScene = GD.Load<PackedScene>("res://AimIndicator.tscn");
        if (aimIndicatorScene != null)
        {
            aimIndicator = aimIndicatorScene.Instantiate<AimIndicator>();
            AddChild(aimIndicator);
            GD.Print("AimIndicator created successfully!");
        }
        else
        {
            // Fallback: create aim indicator manually if scene doesn't exist
            aimIndicator = new AimIndicator();
            AddChild(aimIndicator);
            GD.Print("AimIndicator created manually!");
        }
        
        // Find the ElementBufferUI in the scene
        elementUI = GetNode<ElementBufferUI>("../ElementBufferUi");
        if (elementUI == null)
        {
            elementUI = GetNode<ElementBufferUI>("/root/Main/ElementBufferUi");
        }
        if (elementUI == null)
        {
            elementUI = GetTree().GetFirstNodeInGroup("element_ui") as ElementBufferUI;
        }
        
        if (elementUI == null)
        {
            GD.PrintErr("ElementBufferUI still not found! Check scene structure.");
        }
        else
        {
            GD.Print("ElementBufferUI found successfully!");
        }
        
        // Find the SpellSystem
        spellSystem = GetNode<SpellSystem>("../SpellSystem");
        if (spellSystem == null)
        {
            GD.PrintErr("SpellSystem not found! Make sure it's added to Main scene.");
        }
        else
        {
            GD.Print("SpellSystem found successfully!");
        }
    }
   
    public override void _PhysicsProcess(double delta)
    {
        HandleMovement(delta);
        HandleElementInput();
        HandleSpellCasting();
        HandleAiming();
    }
   
    private void HandleMovement(double delta)
    {
        Vector2 velocity = Velocity;
        
        // Update coyote time
        bool isOnFloor = IsOnFloor();
        if (isOnFloor)
        {
            coyoteTimer = CoyoteTime;
        }
        else if (wasOnFloor && !isOnFloor)
        {
            coyoteTimer = CoyoteTime;
        }
        else
        {
            coyoteTimer -= (float)delta;
        }
        
        wasOnFloor = isOnFloor;
       
        // Apply gravity
        if (!isOnFloor)
            velocity.Y += gravity * (float)delta;
       
        // Handle jumping
        bool canJump = isOnFloor || coyoteTimer > 0;
        if (Input.IsActionJustPressed("ui_up") && canJump)
        {
            velocity.Y = JumpVelocity;
            coyoteTimer = 0;
        }
            
        // Handle drop-through platforms
        if (Input.IsActionJustPressed("ui_down") && isOnFloor && !dropThroughPressed)
        {
            GD.Print("Trying to drop through platform!");
            dropThroughPressed = true;
            
            SetCollisionMaskValue(1, false);
            velocity.Y = 150;
            
            GetTree().CreateTimer(0.1).Timeout += () => {
                SetCollisionMaskValue(1, true);
                dropThroughPressed = false;
            };
        }
       
        // Handle horizontal movement with momentum
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        float inputX = direction.X;
        
        float acceleration = isOnFloor ? GroundAcceleration : AirAcceleration;
        float friction = isOnFloor ? GroundFriction : AirFriction;
        
        if (inputX != 0)
        {
            if (Mathf.Sign(inputX) != Mathf.Sign(velocity.X))
            {
                velocity.X = Mathf.MoveToward(velocity.X, inputX * Speed, (friction + acceleration) * (float)delta);
            }
            else
            {
                velocity.X = Mathf.MoveToward(velocity.X, inputX * Speed, acceleration * (float)delta);
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, friction * (float)delta);
        }
       
        Velocity = velocity;
        MoveAndSlide();
    }
    
    private void HandleElementInput()
    {
        if (elementUI == null) 
        {
            GD.Print("ElementUI is null!");
            return;
        }
        
        // Check for element button presses
        if (Input.IsActionJustPressed("element_air"))
        {
            GD.Print("Air button pressed!");
            elementUI.AddElement("A");
        }
        if (Input.IsActionJustPressed("element_fire"))
        {
            GD.Print("Fire button pressed!");
            elementUI.AddElement("B");
        }
        if (Input.IsActionJustPressed("element_water"))
        {
            GD.Print("Water button pressed!");
            elementUI.AddElement("X");
        }
        if (Input.IsActionJustPressed("element_earth"))
        {
            GD.Print("Earth button pressed!");
            elementUI.AddElement("Y");
        }
    }
    
    private void HandleSpellCasting()
    {
        if (elementUI == null || spellSystem == null) return;
        
        // Check if cast button is pressed
        if (Input.IsActionJustPressed("cast_spell"))
        {
            if (!hasSpellPrepared)
            {
                // First press: Try to prepare spell
                GD.Print("Player: Attempting to prepare spell...");
                
                var currentBuffer = elementUI.GetBuffer();
                var spellData = spellSystem.TryPrepareSpell(currentBuffer);
                
                if (spellData != null)
                {
                    hasSpellPrepared = true;
                    preparedSpell = spellData;
                    elementUI.ClearBuffer();
                    GD.Print($"Player: Spell '{spellData.Name}' prepared! Aim with right stick and press cast again to fire.");
                    
                    // Show aim indicator if we're already aiming
                    Vector2 aimInput = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
                    if (aimInput.Length() > 0.2f && aimIndicator != null)
                    {
                       aimIndicator.ShowAiming(aimInput.Normalized(), spellData.ProjectileStats, spellData.ProjectileCount);   }
                }
                else
                {
                    GD.Print("Player: No valid spell found in buffer.");
                    elementUI.ClearBuffer();
                }
            }
            else
            {
                // Second press: Fire the prepared spell
                GD.Print($"Player: Firing prepared spell '{preparedSpell.Name}' in direction {AimDirection}");
                
                spellSystem.CastPreparedSpell(preparedSpell, GlobalPosition, AimDirection);
                
                // Hide aim indicator and reset spell preparation state
                if (aimIndicator != null)
                {
                    aimIndicator.HideAiming();
                }
                hasSpellPrepared = false;
                preparedSpell = null;
            }
        }
        
        // Allow canceling prepared spell with different button
        if (Input.IsActionJustPressed("ui_cancel") && hasSpellPrepared)
        {
            GD.Print("Player: Cancelled prepared spell.");
            if (aimIndicator != null)
            {
                aimIndicator.HideAiming();
            }
            hasSpellPrepared = false;
            preparedSpell = null;
        }
    }
   
    private void HandleAiming()
    {
        // Get aim input from right stick
        Vector2 aimInput = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
       
        // Only update aim direction if stick is moved (deadzone handling)
        if (aimInput.Length() > 0.2f)
        {
            AimDirection = aimInput.Normalized();
            
            // Show/update aim indicator only when spell is prepared
            if (hasSpellPrepared && aimIndicator != null)
            {
               aimIndicator.ShowAiming(AimDirection, preparedSpell.ProjectileStats, preparedSpell.ProjectileCount);
aimIndicator.UpdateDirection(AimDirection, preparedSpell.ProjectileStats, preparedSpell.ProjectileCount);  }
        }
        else
        {
            // Hide aim indicator when not actively aiming
            if (aimIndicator != null)
            {
                aimIndicator.HideAiming();
            }
        }
    }
    
    // Public method to check if player has a spell prepared (for UI feedback)
    public bool HasSpellPrepared()
    {
        return hasSpellPrepared;
    }
    
    public string GetPreparedSpellName()
    {
        return preparedSpell?.Name ?? "";
    }
}