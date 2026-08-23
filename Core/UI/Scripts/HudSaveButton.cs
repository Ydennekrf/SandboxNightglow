using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class HudSaveButton : Button
    {
        private const string DefaultText = "Save";

        public override void _Ready()
        {
            Pressed += OnPressed;
        }

        private async void OnPressed()
        {
            GameManager gameManager = GameManager.Instance;
            GD.Print("[SaveLoad] HUD save button clicked.");

            if (gameManager == null)
            {
                await ShowTemporaryText("No Manager");
                return;
            }

            if (gameManager.ActiveSaveSlot <= 0)
            {
                gameManager.SaveCurrentGame();
                await ShowTemporaryText("No Slot");
                return;
            }

            gameManager.SaveCurrentGame();
            await ShowTemporaryText($"Saved {gameManager.ActiveSaveSlot}");
        }

        private async Task ShowTemporaryText(string text)
        {
            Disabled = true;
            Text = text;
            await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);
            Text = DefaultText;
            Disabled = false;
        }
    }
}
