#if !NJCONSOLE_DISABLE
using Ninjadini.Console.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Editor
{
    [CustomPropertyDrawer(typeof(SecretPassAccessChallenge))]
    class ConsoleOverlayKeyPassDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var foldout = new Foldout { text = ConsoleEditorSettingsUI.FormatModuleName(typeof(SecretPassAccessChallenge)), value = property.isExpanded };
            container.Add(foldout);
            
            foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);

            var hashProp = property.FindPropertyRelative(nameof(SecretPassAccessChallenge.passwordHash));
            
            var hashField = new PropertyField(hashProp, "Hashed Secret");
            hashField.SetEnabled(false);
            foldout.Add(hashField);

            var secretInput = new TextField("Enter New Secret");
            secretInput.isPasswordField = true;
            foldout.Add(secretInput);

            var button = new Button(() =>
            {
                if (string.IsNullOrEmpty(secretInput.value))
                {
                    hashProp.stringValue = "";
                }
                else
                {
                    hashProp.stringValue = SecretPassAccessChallenge.HashPassword(secretInput.value);
                }
                property.serializedObject.ApplyModifiedProperties();
                secretInput.value = "";
            })
            {
                text = "Set New Secret Passcode",
                style = { marginLeft = 140, marginBottom = 10, width = 160 }
            };
            foldout.Add(button);

            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(SecretPassAccessChallenge.keyboardType))));
            
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(SecretPassAccessChallenge.hintMessage))));

            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(SecretPassAccessChallenge.requiredInEditor))));

            var warning = new Label("⚠ Although passcode is hashed, it is not totally secure.\nYou should use other means to lock out sensitive features for production, such as by disabling NjConsole.");
            warning.style.color = new StyleColor(new Color(1f, 0.7f, 0.0f));
            warning.style.unityFontStyleAndWeight = FontStyle.Bold;
            foldout.Add(warning);
            
            return container;
        }
    }
}
#endif