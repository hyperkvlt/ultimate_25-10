using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    /// <summary>
    /// Add your own custom panel using OnGUI rendering.<br/>
    /// You can use IConsoleExtension to auto start or start manually like this:<br/>
    /// NjConsole.Modules.AddModule(new DemoOnGUIPanel());<br/>
    /// See documentation for more info about IConsoleExtension.
    /// <code>
    /// public class DemoOnGUIPanelModule : IConsoleIMGUIPanelModule
    /// {
    ///  public string Name => "OnGUI";
    /// 
    ///  public float SideBarOrder => 12;
    /// 
    ///  public IConsoleIMGUI CreateIMGUIPanel(ConsoleContext context)
    ///  {
    ///     // Each ConsoleContext is a different ui window, so we need different instance per window.
    ///     return new BasicOnGUIDemoPanel();
    ///  }
    /// }
    /// 
    /// class BasicOnGUIDemoPanel : IConsoleIMGUI
    /// {
    ///  public void OnGUI()
    ///  {
    ///      GUILayout.Label("Hello from OnGUI");
    ///  }
    /// }
    /// </code>
    /// </summary>
    public interface IConsoleIMGUI
    {
        void OnShown(ConsoleContext context)
        {
        }
        
        Color BackgroundColor => new Color(0.1f, 0.1f, 0.12f);

        void OnGUI();

        void OnHidden(ConsoleContext context)
        {
        }
    }
    /// <summary>
    /// See documentation of IConsoleIMGUI for more details.
    /// </summary>
    public interface IConsoleIMGUIPanelModule : IConsolePanelModule
    {
        IConsoleIMGUI CreateIMGUIPanel(ConsoleContext context);
        
        VisualElement IConsolePanelModule.CreateElement(ConsoleContext context)
        {
#if !NJCONSOLE_DISABLE
            var imgui = CreateIMGUIPanel(context);
            if (imgui == null)
            {
                return null;
            }
            return new OnGUIAdaptorElement(context, imgui);
#else
            return null;
#endif
        }

#if !NJCONSOLE_DISABLE
        class OnGUIAdaptorElement : VisualElement
        {
            ConsoleContext _context;
            IConsoleIMGUI _imgui;
            IMGUIContainer _editorContainer;
            OnGUIAdaptor _adaptor;

            public OnGUIAdaptorElement(ConsoleContext context, IConsoleIMGUI imgui)
            {
                _context = context;
                _imgui = imgui;

                var color = imgui.BackgroundColor;
                if (color == Color.clear)
                {
                    style.backgroundColor = StyleKeyword.None;
                }
                else
                {
                    style.backgroundColor = color;
                }

                if (!context.RuntimeUIDocument)
                {
                    _editorContainer = new IMGUIContainer(imgui.OnGUI);
                    _editorContainer.style.flexGrow = 1f;
                    _editorContainer.style.flexShrink = 0f;
                    Add(_editorContainer);
                }
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                if (_editorContainer != null)
                {
                    _imgui.OnShown(_context);
                    return;
                }
                if (!_context.RuntimeUIDocument) return;
                if (!_adaptor)
                {
                    _adaptor = _context.RuntimeUIDocument.GetComponent<OnGUIAdaptor>();
                    if (!_adaptor)
                    {
                        _adaptor = _context.RuntimeUIDocument.gameObject.AddComponent<OnGUIAdaptor>();
                    }
                }
                _adaptor.Element = this;
                _adaptor.enabled = true;
                _imgui.OnShown(_context);
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                if (_editorContainer != null)
                {
                    _imgui.OnHidden(_context);
                    return;
                }
                if (_adaptor && _adaptor.Element == this)
                {
                    _adaptor.Element = null;
                    _adaptor.enabled = false;
                }
                _imgui.OnHidden(_context);
            }

            public Rect RootWorldBound => _context.RootElement.worldBound;

            public void OnGui()
            {
                _imgui?.OnGUI();
            }
        }

        class OnGUIAdaptor : MonoBehaviour
        {
            public OnGUIAdaptorElement Element;
            
            void OnGUI()
            {
                if (Element == null)
                {
                    enabled = false;
                    return;
                }

                var worldBound = Element.worldBound;
                var rootWorldBound = Element.RootWorldBound;
                var screenScale = new Vector2(Screen.width / rootWorldBound.xMax, Screen.height / rootWorldBound.yMax);

                var pos = worldBound.position;
                pos.x += 2f;
                var size = worldBound.size;
                size.x -= 4f;
                var rect = new Rect(pos * screenScale, size * screenScale);
                
                GUI.BeginGroup(rect);
                {
                    //GUI.BeginClip(new Rect(0, 0, rect.width, rect.height));
                    Element.OnGui();
                    //GUI.EndClip();
                }
                GUI.EndGroup();
            }
        }
#endif
    }
}