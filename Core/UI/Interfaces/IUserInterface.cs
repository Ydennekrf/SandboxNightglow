

using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public interface IUserInterface
    {
        public List<IUIRefresh> UINodeList { get; set; }
        void RefreshUI();
        void ShowOnlyHud();
        void ShowOnlyMainMenu();
        void ShowPlayerMenu(bool show);
        void TogglePlayerMenu();
    }
}