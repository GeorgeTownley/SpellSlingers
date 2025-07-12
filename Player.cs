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
    private bool isChargingSpell = false;
    private float chargeTime = 0.0f;
    private const float MaxChargeTime = 2.0f;
    private const float ChargedMovementMultiplier = 0.2f;
	private IceLance[] telegraphProjectiles = null;
    
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
       
        // Handle jumping - reduced jump height while charging
        bool canJump = isOnFloor || coyoteTimer > 0;
        if (Input.IsActionJustPressed("ui_up") && canJump)
        {
            float jumpMultiplier = isChargingSpell ? 0.2f : 1.0f; // Much weaker jump while charging
            velocity.Y = JumpVelocity * jumpMultiplier;
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
        
        // Reduce movement speed while charging spells
        float speedMultiplier = isChargingSpell ? ChargedMovementMultiplier : 1.0f;
        
        if (inputX != 0)
        {
            if (Mathf.Sign(inputX) != Mathf.Sign(velocity.X))
            {
                velocity.X = Mathf.MoveToward(velocity.X, inputX * Speed * speedMultiplier, (friction + acceleration) * (float)delta);
            }
            else
            {
                velocity.X = Mathf.MoveToward(velocity.X, inputX * Speed * speedMultiplier, acceleration * (float)delta);
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
                        aimIndicator.ShowAiming(aimInput.Normalized(), spellData.ProjectileStats, spellData.ProjectileCount, 1.0f, 1.0f);
                    }
                }
                else
                {
                    GD.Print("Player: No valid spell found in buffer.");
                    elementUI.ClearBuffer();
                }
            }
            else if (!isChargingSpell)
            {
                // Second press: Start charging spell
                isChargingSpell = true;
                chargeTime = 0.0f;
                
                // Show telegraph projectiles
                float initialSpreadMultiplier = 1.0f;
                telegraphProjectiles = spellSystem.ShowChargeTelegraph(preparedSpell, GlobalPosition, AimDirection, initialSpreadMultiplier);
                
                GD.Print("Player: Charging spell...");
            }
        }
        
        // Handle charging logic
        if (hasSpellPrepared && isChargingSpell)
        {
            if (Input.IsActionPressed("cast_spell"))
            {
                // Continue charging
                chargeTime += (float)GetPhysicsProcessDeltaTime();
                chargeTime = Mathf.Min(chargeTime, MaxChargeTime);
                
                // Update aim indicator with current charge in real-time
                if (aimIndicator != null)
                {
                    float chargeSpeedMultiplier = 1.0f + (chargeTime / MaxChargeTime);
                    float chargeSpreadMultiplier = 1.0f - (chargeTime / MaxChargeTime * 0.5f);
                    GD.Print($"Player: Charging - Time: {chargeTime:F2}s, Speed: {chargeSpeedMultiplier:F2}x, Spread: {chargeSpreadMultiplier:F2}x");
                    aimIndicator.UpdateDirection(AimDirection, preparedSpell.ProjectileStats, preparedSpell.ProjectileCount, chargeSpeedMultiplier, chargeSpreadMultiplier);
                }
                
                // Update telegraph positions with current spread
                if (telegraphProjectiles != null)
                {
                    spellSystem.HideChargeTelegraph(telegraphProjectiles);
                    float chargeSpreadMultiplier = 1.0f - (chargeTime / MaxChargeTime * 0.5f);
                    telegraphProjectiles = spellSystem.ShowChargeTelegraph(preparedSpell, GlobalPosition, AimDirection, chargeSpreadMultiplier);
                }
            }
            else
            {
                // Released trigger: Fire the charged spell
                float chargeMultiplier = 1.0f + (chargeTime / MaxChargeTime); // 1.0x to 2.0x speed
                float spreadMultiplier = 1.0f - (chargeTime / MaxChargeTime * 0.5f); // 1.0x to 0.5x spread
                
                GD.Print($"Player: Firing charged spell '{preparedSpell.Name}' with {chargeMultiplier:F2}x speed, {spreadMultiplier:F2}x spread");
                
                // Hide telegraph projectiles
                if (telegraphProjectiles != null)
                {
                    spellSystem.HideChargeTelegraph(telegraphProjectiles);
                    telegraphProjectiles = null;
                }
                
                spellSystem.CastPreparedSpell(preparedSpell, GlobalPosition, AimDirection, chargeMultiplier, spreadMultiplier);
                
                // Reset everything
                if (aimIndicator != null)
                {
                    aimIndicator.HideAiming();
                }
                hasSpellPrepared = false;
                preparedSpell = null;
                isChargingSpell = false;
                chargeTime = 0.0f;
            }
        }
        
        // Allow canceling prepared spell with different button
        if (Input.IsActionJustPressed("ui_cancel") && hasSpellPrepared)
        {
            GD.Print("Player: Cancelled prepared spell.");
            
            // Hide telegraph projectiles
            if (telegraphProjectiles != null)
            {
                spellSystem.HideChargeTelegraph(telegraphProjectiles);
                telegraphProjectiles = null;
            }
            
            if (aimIndicator != null)
            {
                aimIndicator.HideAiming();
            }
            hasSpellPrepared = false;
            preparedSpell = null;
            isChargingSpell = false;
            chargeTime = 0.0f;
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
        }
        
        // Show/update aim indicator only when spell is prepared
        if (hasSpellPrepared && aimIndicator != null && aimInput.Length() > 0.2f)
        {
            // Calculate current charge multipliers
            float chargeSpeedMultiplier = 1.0f;
            float chargeSpreadMultiplier = 1.0f;
            
            if (isChargingSpell)
            {
                chargeSpeedMultiplier = 1.0f + (chargeTime / MaxChargeTime); // 1.0x to 2.0x
                chargeSpreadMultiplier = 1.0f - (chargeTime / MaxChargeTime * 0.5f); // 1.0x to 0.5x
            }
            
            aimIndicator.ShowAiming(AimDirection, preparedSpell.ProjectileStats, preparedSpell.ProjectileCount, chargeSpeedMultiplier, chargeSpreadMultiplier);
        }
        else if (aimInput.Length() <= 0.2f)
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
    
    // Damage system for projectile impacts
    public void TakeDamage(float damage)
    {
        GD.Print($"Player took {damage:F1} damage!");
        
        // TODO: Implement health system, knockback, invincibility frames, etc.
        // For now just log the damage
    }
}