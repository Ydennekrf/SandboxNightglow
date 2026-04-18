using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
	public partial class UIManager : IResolveable, IUserInterface
	{
		private int _resolveOrder = 100;
		private List<IUIRefresh> _uiNodeList;
		public int ResolveOrder => _resolveOrder;

		public List<IUIRefresh> UINodeList { get => _uiNodeList; set => _uiNodeList = value; }

		private UIRoot _root;

		public void Initialize(UIRoot root)
		{
			_root = root;
			_uiNodeList = new List<IUIRefresh>();
		}

		public void ShowOnlyHud() => _root?.ShowOnlyHud();
		public void ShowOnlyMainMenu() => _root?.ShowOnlyMainMenu();
		public void ShowOnlyPlayerMenu() => _root?.ShowOnlyPlayerMenu();

		public void Register(IUIRefresh ui)
		{
			if (!_uiNodeList.Contains(ui))
				_uiNodeList.Add(ui);
		}

		//=========IUserInterface==========//

		public void RefreshUI()
		{

			if (_uiNodeList == null || _uiNodeList.Count == 0)
				return;

			foreach (IUIRefresh control in _uiNodeList)
			{
				if (control.needsRefresh)
				{
					control.Refresh();
				}
			}
		}

		//=========IResolvable=========//

		public void Resolve()
		{
			RefreshUI();
		}

		public void Resolve(object obj)
		{
			
		}
	}
}
