using UnityEditor;
using UnityEngine.UIElements;

namespace GBS.Utility
{
    public static class GBStyleUtility
    {
        public static VisualElement AddClasses(this VisualElement element, params string[] classNames)
        {
            foreach (var className in classNames)
            {
                element.AddToClassList(className);
            }

            return element;
        }

        public static VisualElement AddVisualElement(this VisualElement element, params string[] styleSheetNumes)
        {
            foreach (string styleSheetName in styleSheetNumes)
            {
                StyleSheet styleSheet = (StyleSheet)EditorGUIUtility.Load(styleSheetName); //не работает визуал, хз поч
                //https://www.youtube.com/watch?v=A0amLS4LWyc&list=PL0yxB6cCkoWK38XT4stSztcLueJ_kTx5f&index=14&ab_channel=IndieWafflus

                element.styleSheets.Add(styleSheet);
            }

            return element;
        }
    }
}
