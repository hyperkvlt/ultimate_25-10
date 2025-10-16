#if !NJCONSOLE_DISABLE
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class ConsoleHierarchyPanel : VisualElement, IConsolePanelModule.IElement
    {
        readonly ConsoleContext _context;

        readonly ScrollView _treeView;
        ConsoleInspector _inspector;
        ChildElement _selectedElement;

        GameObject _dontDestroyOnLoad;
        Scene? _dontDestroyOnLoadScene;

        void IConsolePanelModule.IElement.OnReselected()
        {
            Reset();
        }
        
        public ConsoleHierarchyPanel(ConsoleContext context)
        {
            _context = context;
            
            AddToClassList("hierarchy-panel");
            
            VisualElement window = context.Window != null ? context.Window : this;
            ConsoleUIUtils.RegisterOrientationDirection(window, this, FlexDirection.Row, FlexDirection.Column);
            
            _treeView = new ScrollView();
            ConsoleUIUtils.RegisterOrientationClass(window, _treeView, "hierarchy-tree", "hierarchy-tree-portrait");
            Add(_treeView);

            schedule.Execute(Update).Every(0);
            
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if(evt.keyCode == KeyCode.Escape)
                {
                    _inspector?.RemoveFromHierarchy();
                }
            });
        }

        public void Reset()
        {
            _inspector?.RemoveFromHierarchy();
        }

        List<(Scene scene, ChildElement element)> _scenes = new();
        
        List<GameObject> tempList = new List<GameObject>();

        void Update()
        {
            for (int sceneIndex = 0, sceneCount = SceneManager.sceneCount; sceneIndex < sceneCount; sceneIndex++)
            {
                UpdateScene(SceneManager.GetSceneAt(sceneIndex));
            }
            for(var i = _scenes.Count - 1; i >= 0; i--)
            {
                if (!_scenes[i].scene.IsValid())
                {
                    _scenes[i].element.RemoveFromHierarchy();
                    _scenes.RemoveAt(i);
                }
            }
            if (Application.isPlaying)
            {
                if (!_dontDestroyOnLoad)
                {
                    _dontDestroyOnLoad = new GameObject("ConsoleHierarchyProbe");
                    Object.DontDestroyOnLoad(_dontDestroyOnLoad);
                }
                var scene = _dontDestroyOnLoad.scene;
                if (scene.IsValid())
                {
                    UpdateScene(scene);
                }
                _dontDestroyOnLoadScene = scene;
            }
            else if (_dontDestroyOnLoadScene.HasValue)
            {
                var scene = _scenes.Find(a => a.scene == _dontDestroyOnLoadScene);
                if (scene.element != null)
                {
                    scene.element.RemoveFromHierarchy();
                    _scenes.Remove(scene);
                }
                _dontDestroyOnLoadScene = null;
            }
        }

        void UpdateScene(Scene scene)
        {
            scene.GetRootGameObjects(tempList);
            var found = false;
            foreach (var sceneObj in _scenes)
            {
                if (sceneObj.scene == scene)
                {
                    found = true;
                    sceneObj.element.Update(tempList);
                    break;
                }
            }
            if (!found)
            {
                var element = new ChildElement(this, false);
                _scenes.Add((scene, element));
                element.SetupForScene(scene);
                element.Update(tempList);
                _treeView.Add(element);
            }
            tempList.Clear();
        }

        void ClickedElement(ChildElement element)
        {
            _selectedElement?.SetSelected(false);
            _selectedElement = element;
            if (element != null)
            {
                element.SetSelected(true);
                if (_inspector == null)
                {
                    _inspector = new ConsoleInspector();
                    _inspector.AddToClassList("panel");
                    _inspector.style.flexGrow = 1f;
                    _inspector.style.minHeight = Length.Percent(60);
                    Add(_inspector);
                }

                if (_inspector.parent != this)
                {
                    // because the user can close it.
                    Add(_inspector);
                }
                if (element.GameObject)
                {
                    _inspector.Inspect(element.GameObject);
                }
                else
                {
                    _inspector.Inspect(SceneManager.GetSceneByName(element.name));
                }
            }
        }

        class ChildElement : VisualElement
        {
            readonly ConsoleHierarchyPanel _parent;
            readonly Button _arrowBtn;
            readonly Button _itemBtn;
            readonly VisualElement _tree;
            bool _expanded;
            
            public GameObject GameObject;

            static readonly List<GameObject> AlwaysEmptyList = new List<GameObject>();

            public ChildElement(ConsoleHierarchyPanel parent, bool forGameObjects)
            {
                _parent = parent;
                var horizontal = new VisualElement();
                horizontal.style.flexDirection = FlexDirection.Row;
                Add(horizontal);

                _arrowBtn = new Button(OnArrowClicked)
                {
                    text = "\u25bc"
                };
                _arrowBtn.AddToClassList("hierarchy-item-arrow-btn");
                horizontal.Add(_arrowBtn);
                _itemBtn = new Button(OnItemClicked);
                _itemBtn.AddToClassList("hierarchy-item-item-btn");
                horizontal.Add(_itemBtn);

                _tree = new VisualElement();
                Add(_tree);
                
                if (forGameObjects)
                {
                    AddToClassList("hierarchy-item-gameobject");
                }
                else
                {
                    _expanded = true;
                    AddToClassList("hierarchy-item-scene");
                    UpdateExpandArrow();
                }
            }

            public void SetupForScene(Scene scene)
            {
                name = scene.name;
                _itemBtn.text = scene.name;
            }

            void Update()
            {
                if (!GameObject)
                {
                    return;
                }
                UpdateExpandArrow();
                _itemBtn.text = GameObject.name;
                if (!_expanded)
                {
                    Update(AlwaysEmptyList);
                    return;
                }
                var tempList = LoggerUtils.ThreadLocalPool<List<GameObject>>.Borrow();
                tempList.Clear();
                var goTran = GameObject.transform;
                for (int index = 0, goCount = goTran.childCount; index < goCount; index++)
                {
                    tempList.Add(goTran.GetChild(index).gameObject);
                }
                Update(tempList);
                tempList.Clear();
                LoggerUtils.ThreadLocalPool<List<GameObject>>.Return(tempList);
            }

            public void Update(List<GameObject> gameObjects)
            {
                if (!_expanded)
                {
                    gameObjects.Clear();
                }
                for (int index = 0, goCount = gameObjects.Count; index < goCount; index++)
                {
                    var childGo = gameObjects[index];
                    var found = false;
                    var realChildIndex = 0;
                    var targetChildIndex = 0;
                    for (int childIndex = 0, childElementsCount = _tree.childCount; childIndex < childElementsCount; childIndex++)
                    {
                        var otherElement = _tree.ElementAt(childIndex) as ChildElement;
                        if (otherElement == null)
                        {
                            targetChildIndex++;
                            continue;
                        }
                        if (otherElement.GameObject != childGo)
                        {
                            if (index == realChildIndex)
                            {
                                targetChildIndex = childIndex;
                            }
                            realChildIndex++;
                            continue;
                        }
                        found = true;
                        if (realChildIndex != index)
                        {
                            _tree.RemoveAt(childIndex);
                            _tree.Insert(targetChildIndex, otherElement);
                        }
                        otherElement.Update();
                        break;
                    }
                    if (!found)
                    {
                        var element = new ChildElement(_parent, true);
                        element.GameObject = childGo;
                        element.Update();
                        _tree.Insert(targetChildIndex, element);
                    }
                }

                for (int childIndex = _tree.childCount - 1; childIndex >= 0; childIndex--)
                {
                    if (_tree.ElementAt(childIndex) is ChildElement otherElement)
                    {
                        if (!otherElement.GameObject || !gameObjects.Contains(otherElement.GameObject))
                        {
                            _tree.RemoveAt(childIndex);
                        }
                    }
                }
            }

            void OnArrowClicked()
            {
                _expanded = !_expanded;
                UpdateExpandArrow();
            }

            void OnItemClicked()
            {
                _parent.ClickedElement(this);
            }

            public void SetSelected(bool selected)
            {
                if (selected)
                {
                    _itemBtn.AddToClassList("hierarchy-item-item-btn-selected");
                }
                else
                {
                    _itemBtn.RemoveFromClassList("hierarchy-item-item-btn-selected");
                }
            }

            void UpdateExpandArrow()
            {
                if (GameObject && GameObject.transform.childCount == 0)
                {
                    _arrowBtn.style.visibility = Visibility.Hidden;
                }
                else
                {
                    _arrowBtn.style.visibility = Visibility.Visible;
                    _arrowBtn.text = _expanded ? "\u25bc" : "\u25b6";
                }
            }
        }
    }
}
#endif