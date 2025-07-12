using Godot;
using System.Collections.Generic;

public partial class ElementBufferUI : Control
{
    [Export]
    public float BufferTimeout = 3.0f;
    
    private Label bufferLabel;
    private List<string> elementBuffer = new List<string>();
    private float timeSinceLastInput = 0.0f;
    
    public override void _Ready()
    {
        // Create the label for displaying elements
        bufferLabel = new Label();
        bufferLabel.Position = new Vector2(20, 20); // Top-left corner with some padding
        bufferLabel.Size = new Vector2(400, 100); // Give it a size
        
        // Make text bold, larger, and white
        bufferLabel.AddThemeFontSizeOverride("font_size", 32);
        bufferLabel.AddThemeColorOverride("font_color", Colors.White);
        bufferLabel.Text = "TEST"; // Start with test text
        
        AddChild(bufferLabel);
        
        GD.Print("ElementBufferUI ready!");
    }
    
    public override void _Process(double delta)
    {
        // Count down timer
        if (elementBuffer.Count > 0)
        {
            timeSinceLastInput += (float)delta;
            
            // Clear buffer if timeout reached
            if (timeSinceLastInput >= BufferTimeout)
            {
                ClearBuffer();
            }
        }
    }
    
    public void AddElement(string elementLetter)
    {
        elementBuffer.Add(elementLetter);
        timeSinceLastInput = 0.0f; // Reset timer
        UpdateDisplay();
        
        GD.Print($"Added element: {elementLetter}, Buffer: {string.Join("", elementBuffer)}");
    }
    
    public void ClearBuffer()
    {
        elementBuffer.Clear();
        timeSinceLastInput = 0.0f;
        UpdateDisplay();
        
        GD.Print("Buffer cleared!");
    }
    
    private void UpdateDisplay()
    {
        if (bufferLabel != null)
        {
            bufferLabel.Text = string.Join(" ", elementBuffer);
        }
    }
    
    public List<string> GetBuffer()
    {
        return new List<string>(elementBuffer); // Return copy
    }
}