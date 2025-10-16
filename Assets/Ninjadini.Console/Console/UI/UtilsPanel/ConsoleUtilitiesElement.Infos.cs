#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleUtilitiesElement
    {
        // we need to do this due to player builds losing the properties due to native calls.
        static readonly (string name, Type type, Func<object> getter, Action<object> setter)[] QualitySettingFields = new (string, Type, Func<object>, Action<object>)[]
        {
            (nameof(QualitySettings.vSyncCount), typeof(int), () => QualitySettings.vSyncCount, (v) => QualitySettings.vSyncCount = (int)v),
            (nameof(QualitySettings.antiAliasing), typeof(int), () => QualitySettings.antiAliasing, (v) => QualitySettings.antiAliasing = (int)v),
            (nameof(QualitySettings.anisotropicFiltering), typeof(AnisotropicFiltering), () => QualitySettings.anisotropicFiltering, (v) => QualitySettings.anisotropicFiltering = (AnisotropicFiltering)v),
            (nameof(QualitySettings.activeColorSpace), typeof(ColorSpace), () => QualitySettings.activeColorSpace, null),
            default,
            (nameof(QualitySettings.asyncUploadPersistentBuffer), typeof(bool), () => QualitySettings.asyncUploadPersistentBuffer, (v) => QualitySettings.asyncUploadPersistentBuffer = (bool)v),
            (nameof(QualitySettings.asyncUploadTimeSlice), typeof(int), () => QualitySettings.asyncUploadTimeSlice, (v) => QualitySettings.asyncUploadTimeSlice = (int)v),
            (nameof(QualitySettings.asyncUploadBufferSize), typeof(int), () => QualitySettings.asyncUploadBufferSize, (v) => QualitySettings.asyncUploadBufferSize = (int)v),
            default,
            (nameof(QualitySettings.lodBias), typeof(float), () => QualitySettings.lodBias, (v) => QualitySettings.lodBias = (float)v),
            (nameof(QualitySettings.maximumLODLevel), typeof(int), () => QualitySettings.maximumLODLevel, (v) => QualitySettings.maximumLODLevel = (int)v),
#if UNITY_2022_3_OR_NEWER
            (nameof(QualitySettings.globalTextureMipmapLimit), typeof(int), () => QualitySettings.globalTextureMipmapLimit, (v) => QualitySettings.globalTextureMipmapLimit = (int)v),
#endif
            default,
            (nameof(QualitySettings.shadows), typeof(ShadowQuality), () => QualitySettings.shadows, (v) => QualitySettings.shadows = (ShadowQuality)v),
            (nameof(QualitySettings.shadowCascades), typeof(int), () => QualitySettings.shadowCascades, (v) => QualitySettings.shadowCascades = (int)v),
            (nameof(QualitySettings.shadowDistance), typeof(float), () => QualitySettings.shadowDistance, (v) => QualitySettings.shadowDistance = (float)v),
            (nameof(QualitySettings.shadowProjection), typeof(ShadowProjection), () => QualitySettings.shadowProjection, (v) => QualitySettings.shadowProjection = (ShadowProjection)v),
            (nameof(QualitySettings.shadowmaskMode), typeof(ShadowmaskMode), () => QualitySettings.shadowmaskMode, (v) => QualitySettings.shadowmaskMode = (ShadowmaskMode)v),
            (nameof(QualitySettings.pixelLightCount), typeof(int), () => QualitySettings.pixelLightCount, (v) => QualitySettings.pixelLightCount = (int)v),
            default,
            (nameof(QualitySettings.softParticles), typeof(bool), () => QualitySettings.softParticles, (v) => QualitySettings.softParticles = (bool)v),
            (nameof(QualitySettings.particleRaycastBudget), typeof(int), () => QualitySettings.particleRaycastBudget, (v) => QualitySettings.particleRaycastBudget = (int)v),
            (nameof(QualitySettings.softVegetation), typeof(bool), () => QualitySettings.softVegetation, (v) => QualitySettings.softVegetation = (bool)v),
            default,
            (nameof(QualitySettings.streamingMipmapsMemoryBudget), typeof(float), () => QualitySettings.streamingMipmapsMemoryBudget, (v) => QualitySettings.streamingMipmapsMemoryBudget = (float)v),
            (nameof(QualitySettings.streamingMipmapsMaxLevelReduction), typeof(int), () => QualitySettings.streamingMipmapsMaxLevelReduction, (v) => QualitySettings.streamingMipmapsMaxLevelReduction = (int)v),
            (nameof(QualitySettings.streamingMipmapsMaxFileIORequests), typeof(int), () => QualitySettings.streamingMipmapsMaxFileIORequests, (v) => QualitySettings.streamingMipmapsMaxFileIORequests = (int)v),
        };
        
        static readonly (string name, Func<String> getter)[] DeviceSysInfoFields = new (string, Func<String>)[]
        {
            (nameof(SystemInfo.deviceName), () => SystemInfo.deviceName),
            (nameof(SystemInfo.deviceUniqueIdentifier), () => SystemInfo.deviceUniqueIdentifier),
            (nameof(SystemInfo.operatingSystem), () => SystemInfo.operatingSystem),
            (nameof(SystemInfo.operatingSystemFamily), () => SystemInfo.operatingSystemFamily.ToString()),
            (nameof(SystemInfo.deviceType), () => SystemInfo.deviceType.ToString()),
            (nameof(SystemInfo.deviceModel), () => SystemInfo.deviceModel),
            default,
            (nameof(SystemInfo.batteryLevel), () => (SystemInfo.batteryLevel * 100f).ToString("0.##") + "%"),
            (nameof(SystemInfo.batteryStatus), () => SystemInfo.batteryStatus.ToString()),
            default,
            (nameof(SystemInfo.systemMemorySize), () => ToStr(SystemInfo.systemMemorySize)),
            (nameof(SystemInfo.graphicsMemorySize), () => ToStr(SystemInfo.graphicsMemorySize)),
            (nameof(SystemInfo.processorFrequency), () => ToStr(SystemInfo.processorFrequency)),
            (nameof(SystemInfo.processorCount), () => ToStr(SystemInfo.processorCount)),
            (nameof(SystemInfo.processorType), () => SystemInfo.processorType),
#if UNITY_6000
            (nameof(SystemInfo.processorModel), () => SystemInfo.processorModel),
            (nameof(SystemInfo.processorManufacturer), () => SystemInfo.processorManufacturer),
#endif
            default,
            (nameof(SystemInfo.supportsAudio), () => ToStr(SystemInfo.supportsAudio)),
            (nameof(SystemInfo.supportsAccelerometer), () => ToStr(SystemInfo.supportsAccelerometer)),
            (nameof(SystemInfo.supportsGyroscope), () => ToStr(SystemInfo.supportsGyroscope)),
            (nameof(SystemInfo.supportsVibration), () => ToStr(SystemInfo.supportsVibration)),
            (nameof(SystemInfo.supportsLocationService), () => ToStr(SystemInfo.supportsLocationService)),
        };
        
        static readonly (string name, Func<String> getter)[] GraphicsSysInfoFields = new (string, Func<String>)[]
        {
            (nameof(SystemInfo.graphicsMemorySize), () => ToStr(SystemInfo.graphicsMemorySize)),
            (nameof(SystemInfo.maxTextureSize), () => ToStr(SystemInfo.maxTextureSize)),
            (nameof(SystemInfo.graphicsShaderLevel), () => ToStr(SystemInfo.graphicsShaderLevel)),
            (nameof(SystemInfo.graphicsMultiThreaded), () => ToStr(SystemInfo.graphicsMultiThreaded)),
            (nameof(SystemInfo.supportsInstancing), () => ToStr(SystemInfo.supportsInstancing)),
            default,
            (nameof(SystemInfo.graphicsUVStartsAtTop), () => ToStr(SystemInfo.graphicsUVStartsAtTop)),
            (nameof(SystemInfo.npotSupport), () => SystemInfo.npotSupport.ToString()),
            (nameof(SystemInfo.supportsRayTracing), () => ToStr(SystemInfo.supportsRayTracing)),
            (nameof(SystemInfo.supportsShadows), () => ToStr(SystemInfo.supportsShadows)),
            (nameof(SystemInfo.supportsRawShadowDepthSampling), () => ToStr(SystemInfo.supportsRawShadowDepthSampling)),
            (nameof(SystemInfo.supports3DTextures), () => ToStr(SystemInfo.supports3DTextures)),
            (nameof(SystemInfo.supports3DRenderTextures), () => ToStr(SystemInfo.supports3DRenderTextures)),
            (nameof(SystemInfo.supports2DArrayTextures), () => ToStr(SystemInfo.supports2DArrayTextures)),
            (nameof(SystemInfo.supportsCubemapArrayTextures), () => ToStr(SystemInfo.supportsCubemapArrayTextures)),
            default,
            (nameof(SystemInfo.graphicsDeviceName), () => SystemInfo.graphicsDeviceName),
            (nameof(SystemInfo.graphicsDeviceVersion), () => SystemInfo.graphicsDeviceVersion),
            (nameof(SystemInfo.graphicsDeviceType), () => SystemInfo.graphicsDeviceType.ToString()),
            (nameof(SystemInfo.graphicsDeviceVendor), () => SystemInfo.graphicsDeviceVendor),
            (nameof(SystemInfo.graphicsDeviceID), () => SystemInfo.graphicsDeviceID.ToString()),
            (nameof(SystemInfo.graphicsDeviceVendorID), () => SystemInfo.graphicsDeviceVendorID.ToString()),
        };

        static string ToStr(bool v)
        {
            return v ? "YES" : "NO";
        }

        static string ToStr(int v)
        {
            return v.ToString("N0");
        }
        
        public static VisualElement CreateAppInfoPanel(ConsoleContext context)
        {
            var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            container.AddToClassList("info-info-panel");
            
            AddStatic(container, nameof(Application.persistentDataPath), Application.persistentDataPath);
            AddStatic(container, nameof(Application.streamingAssetsPath), Application.streamingAssetsPath);
            AddStatic(container, nameof(Application.temporaryCachePath), Application.temporaryCachePath);
            AddStatic(container, nameof(Application.dataPath), Application.dataPath);
            AddStatic(container, nameof(Application.absoluteURL), Application.absoluteURL);
            AddStatic(container, nameof(Application.consoleLogPath), Application.consoleLogPath);
            AddLineBreak(container);
            AddDynamic(container, nameof(Application.targetFrameRate), typeof(int),  () => Application.targetFrameRate, (v) => Application.targetFrameRate = (int)v);
            AddDynamic(container, nameof(Application.internetReachability), typeof(NetworkReachability),  () => Application.internetReachability);
            AddLineBreak(container);
            AddStatic(container, nameof(Application.buildGUID), Application.buildGUID);
            AddStatic(container, nameof(Application.platform), Application.platform.ToString());
            AddStatic(container, nameof(Application.productName), Application.productName);
            AddStatic(container, nameof(Application.identifier), Application.identifier);
            AddStatic(container, nameof(Application.version), Application.version);
            AddStatic(container, nameof(Application.companyName), Application.companyName);
            AddLineBreak(container);
            AddStatic(container, nameof(Application.installerName), Application.installerName);
            AddStatic(container, nameof(Application.installMode), Application.installMode.ToString());
            AddStatic(container, nameof(Application.sandboxType), Application.sandboxType.ToString());
            AddStatic(container, nameof(Application.genuine), Application.genuine.ToString());
            AddStatic(container, nameof(Application.genuineCheckAvailable), Application.genuineCheckAvailable.ToString());
            //AddStatic(container, nameof(Application.cloudProjectId), Application.cloudProjectId);

            TryAddShowAllInspector(context, container, typeof(Application));
            return container;
        }
        
        public static VisualElement CreateScreenInfoPanel(ConsoleContext context)
        {
            var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            container.AddToClassList("info-info-panel");

            const string seeInOverlayMsg = "See in runtime overlay";
            if (context.IsRuntimeOverlay)
            {
                AddDynamic(container, nameof(Screen.width), typeof(int),  () => Screen.width, (v) =>
                {
                    var height = Screen.height;
                    ConsoleToasts.Show(context, $"Tried to set resolution: {v} x {height} -- {Screen.fullScreenMode}");
                    Screen.SetResolution((int)v, height, Screen.fullScreenMode);
                });
                AddDynamic(container, nameof(Screen.height), typeof(int),  () => Screen.height, (v) =>
                {
                    var width = Screen.width;
                    ConsoleToasts.Show(context, $"Tried to set resolution: {width} x {v} -- {Screen.fullScreenMode}");
                    Screen.SetResolution(width, (int)v, Screen.fullScreenMode);
                });
            }
            else
            {
                AddStatic(container, nameof(Screen.width), seeInOverlayMsg);
                AddStatic(container, nameof(Screen.height), seeInOverlayMsg);
            }
            AddDynamic(container, nameof(Screen.dpi), typeof(float),  () => Screen.dpi);
            AddDynamic(container, nameof(Application.targetFrameRate), typeof(int),  () => Application.targetFrameRate, (v) => Application.targetFrameRate = (int)v);
            
            AddDynamic(container, nameof(Screen.currentResolution), typeof(Resolution),  () => Screen.currentResolution);
            AddDynamic(container, nameof(Screen.fullScreen), typeof(bool),  () => Screen.fullScreen,  (v) => Screen.fullScreen = (bool)v);
            AddDynamic(container, nameof(Screen.fullScreenMode), typeof(FullScreenMode),  () => Screen.fullScreenMode,  (v) => Screen.fullScreenMode = (FullScreenMode)v);
            AddDynamic(container, nameof(Screen.orientation), typeof(ScreenOrientation),  () => Screen.orientation,  (v) => Screen.orientation = (ScreenOrientation)v);
            
            if (context.IsRuntimeOverlay)
            {
                AddDynamic(container, nameof(Screen.safeArea), typeof(Rect),  () => Screen.safeArea);
            }
            else
            {
                AddStatic(container, nameof(Screen.safeArea), seeInOverlayMsg);
            }

            var cutOuts = Screen.cutouts;
            if (cutOuts.Length > 0)
            {
                AddText(container, "Cutouts:");
                foreach (var rect in cutOuts)
                {
                    AddStatic(container, "", rect.ToString());
                }
            }
            
            AddDynamic(container, nameof(Screen.brightness), typeof(float),  () => Screen.brightness, (v) => Screen.brightness = (float)v);
            AddDynamic(container, nameof(Screen.sleepTimeout), typeof(int),  () => Screen.sleepTimeout, (v) => Screen.sleepTimeout = (int)v);

            TryAddShowAllInspector(context, container, typeof(Screen));
            return container;
        }

        static void TryAddShowAllInspector(ConsoleContext context, VisualElement container, Type type)
        {
            if (IsInspectorEnabled(context))
            {
                AddButton(container, "Show all",
                    () =>
                    {
                        ConsoleInspector.Show(ConsoleUIUtils.FindConsoleOrRoot(container), type);
                        ConsoleToasts.Show(context, "Some properties may not show in the player due to native binding.");
                    });
            }
        }

        public static VisualElement CreateQualityInfoPanel(ConsoleContext context)
        {
            var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            container.AddToClassList("info-info-panel");

            var element = ConsoleInspector.CreateField(new ConsoleInspector.FieldData()
            {
                Name = nameof(QualitySettings.GetQualityLevel),
                Type = typeof(int),
                Getter = () => QualitySettings.GetQualityLevel(),
                Setter = (v) => QualitySettings.SetQualityLevel((int)v),
            });
            container.Add(element);
            foreach (var prop in QualitySettingFields)
            {
                AddDynamic(container, prop.name, prop.type, prop.getter, prop.setter);
            }
            TryAddShowAllInspector(context, container, typeof(QualitySettings));
            return container;
        }
        
        public static VisualElement CreateDeviceInfoPanel()
        {
            return CreateSubSysInfo(DeviceSysInfoFields);
        }

        public static VisualElement CreateGraphicsInfoPanel()
        {
            return CreateSubSysInfo(GraphicsSysInfoFields);
        }

        static ScrollView CreateSubSysInfo((string name, Func<String> getter)[] list)
        {
            var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            container.AddToClassList("info-info-panel");
            foreach (var prop in list)
            {
                AddDynamic(container, prop.name, typeof(string), prop.getter);
            }

            AddText(container, "See `Other SystemInfo` for missing values");
            return container;
        }

        public static VisualElement CreateOtherSystemInfoPanel()
        {
            var container = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            container.AddToClassList("info-info-panel");

            var skipList = new List<string>();
            skipList.AddRange(DeviceSysInfoFields.Select(s => s.name));
            skipList.AddRange(GraphicsSysInfoFields.Select(s => s.name));
            
            var props = typeof(SystemInfo).GetProperties(BindingFlags.Static | BindingFlags.Public).ToList();
            props.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            foreach (var prop in props)
            {
                if (!prop.CanRead || prop.IsDefined(typeof(ObsoleteAttribute)))
                {
                    continue;
                }
                if (skipList.Contains(prop.Name))
                {
                    continue;
                }
                var localProp = prop;
                AddDynamic(container, prop.Name, prop.PropertyType, () => localProp.GetValue(null));
            }
            
            AddText(container, "See `Device Info` and `Graphics Info` for missing values");
            return container;
        }
        
        static void AddStatic(VisualElement container, string propName, string value)
        {
            var textField = new TextField(propName);
            textField.isReadOnly = true;
            textField.name = propName;
            textField.SetValueWithoutNotify(value);
            textField.AddToClassList("info-info-field");
            textField.AddToClassList("field-readonly");
            container.Add(textField);
        }

        static void AddDynamic(VisualElement container, string propName, Type propType, Func<object> getter,  Action<object> setter = null)
        {
            if (string.IsNullOrEmpty(propName))
            {
                AddLineBreak(container);
                return;
            }
            var element = ConsoleInspector.CreateField(propName, propType, getter, setter);
            if (element != null)
            {
                container.Add(element);
            }
        }
        
        static void AddText(VisualElement container, string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("info-info-field");
            container.Add(lbl);
        }
        static void AddLineBreak(VisualElement container)
        {
            var horizontal = new VisualElement();
            horizontal.AddToClassList("info-info-lineBreak");
            container.Add(horizontal);
        }
    }
}
#endif