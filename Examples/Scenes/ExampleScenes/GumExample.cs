using Gum;
using Gum.Forms.Controls;
using Gum.Themes.Neon;
using Gum.Wireframe;
using Raylib_cs;
using ShapeEngine.Core;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

public class GumExample : ExampleScene
{
    private static GumService GumUi => GumService.Default;

    private Button? button;

    public GumExample()
    {
        Title = "Gum";
        Description = "Basic usage of Gum within ShapeEngine";
    }

    protected override void OnActivate(Scene oldScene)
    {
        base.OnActivate(oldScene);

        GumUi.Initialize();

        // Disable the glow, it doesn't look great right now.
        NeonStyling.ActiveStyle.Colors.Glow = new Color(0, 229, 255, 0);
        NeonStyling.ActiveStyle.Colors.GlowSubtle = new Color(0, 229, 255, 0);
        NeonStyling.ActiveStyle.Colors.GlowMedium = new Color(0, 229, 255, 0);
        NeonStyling.ActiveStyle.Colors.GlowStrong = new Color(0, 229, 255, 0);
        NeonTheme.Apply();
        
        // Keep Gum's canvas, render camera, and input coordinates in the same
        // window-relative coordinate space, including on High-DPI displays.
        GumUi.EnableZoomToWindow(defaultZoom: 1.5f);

        StackPanel panel = new StackPanel();
        panel.Orientation = Orientation.Vertical;
        panel.AddToRoot();
        panel.Anchor(Anchor.TopLeft);
        panel.Spacing = 12f;
        panel.X = 18;
        panel.Y = 72;
        
        button = new Button
        {
            Text = "Click Me",
            Width = 200
        };
        button.Click += HandleButtonClicked;
        panel.AddChild(button);
        
        var checkBox = new CheckBox
        {
            Text = "Check Me",
            Width = 200
        };
        panel.AddChild(checkBox);
        
        var comboBox = new ComboBox();
        comboBox.Width = 200;
        for(int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }
        panel.AddChild(comboBox);
        
        var radioButton = new RadioButton
        {
            Text = "Radio Button 1",
            Width = 200
        };
        panel.AddChild(radioButton);
        
        radioButton = new RadioButton
        {
            Text = "Radio Button 2",
            Width = 200
        };
        panel.AddChild(radioButton);
        
        radioButton = new RadioButton
        {
            Text = "Radio Button 3",
            Width = 200
        };
        panel.AddChild(radioButton);
    }

    protected override void OnDeactivate()
    {
        if (button != null)
        {
            button.Click -= HandleButtonClicked;
            button.RemoveFromRoot();
            button = null;
        }
        
        GumUi.Root.Children.Clear();
        GumUi.Uninitialize();
        
        base.OnDeactivate();
    }

    protected override void OnUpdateExample(GameTime time, ScreenInfo game, ScreenInfo gameUi, ScreenInfo ui)
    {
        GumUi.Update(time.TotalSeconds);
    }

    protected override void OnDrawUIExample(ScreenInfo ui)
    {
        GumUi.Draw();
    }

    private void HandleButtonClicked(object? sender, EventArgs e)
    {
        if (button != null)
        {
            button.Text = $"Clicked\n{DateTime.Now:T}";
        }
    }
}
