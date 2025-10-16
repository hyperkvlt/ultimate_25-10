#if !NJCONSOLE_DISABLE
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleLogsPanel
    {
        public abstract class FilteringPanel : VisualElement
        {
            protected readonly Filtering Filtering;

            ConsoleLogsPanel Panel => Filtering.Panel;

            protected FilteringPanel(Filtering filtering)
            {
                Filtering = filtering;
            }
            
            Button _menuBtn;
            public Button MenuBtn
            {
                get
                {
                    if (_menuBtn == null)
                    {
                        _menuBtn = CreateMenuButton();
                        _menuBtn?.AddToClassList("logs-menus-item");
                    }
                    return _menuBtn;
                }
            }

            protected abstract Button CreateMenuButton();

            public void ToggleShowHide()
            {
                var atBottom = Panel.AtBottom();
                if (Showing)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
                if (atBottom)
                {
                    Panel.ScrollToBottom();
                }
            }

            public bool Showing => parent != null;

            public void Show()
            {
                if (!Showing)
                {
                    Filtering.SetShowingPanel(this);
                }
            }

            public void Hide()
            {
                if (Showing && Filtering.ShowingFilterPanel == this)
                {
                    Filtering.SetShowingPanel(null);
                }
            }

            protected virtual void EnsureElementsExists()
            {
                
            }

            public virtual void OnShown()
            {
                EnsureElementsExists();
                _menuBtn.AddToClassList("logs-menus-item-expanded");
                Panel.CloseDetails();
            }

            public virtual void OnHidden()
            {
                _menuBtn.RemoveFromClassList("logs-menus-item-expanded");
            }

            public virtual void Reset()
            {
                if (Showing)
                {
                    Hide();
                }
            }
        }
    }
}
#endif