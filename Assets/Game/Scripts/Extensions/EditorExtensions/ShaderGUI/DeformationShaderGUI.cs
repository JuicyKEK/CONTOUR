using UnityEngine;
using UnityEditor;

public class DeformationShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Base Material
        GUILayout.Label("Base Material", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_BaseMap");
        DrawProperty(materialEditor, properties, "_BaseColor");
        DrawProperty(materialEditor, properties, "_BumpMap");
        DrawProperty(materialEditor, properties, "_BumpScale");
        DrawProperty(materialEditor, properties, "_Metallic");
        DrawProperty(materialEditor, properties, "_Smoothness");
        DrawProperty(materialEditor, properties, "_OcclusionMap");
        DrawProperty(materialEditor, properties, "_OcclusionStrength");

        EditorGUILayout.Space(20);
        GUILayout.Label("DEFORMATION EFFECTS", EditorStyles.boldLabel);

        // Wave
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_WAVE", "_EnableWave", "Wave Effect",
            "_WaveAmplitude", "_WaveFrequency", "_WaveSpeed", "_WaveDirection");

        // Twist
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_TWIST", "_EnableTwist", "Twist Effect",
            "_TwistAmount", "_TwistSpeed", "_TwistCenter");

        // Pulse
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_PULSE", "_EnablePulse", "Pulse Effect",
            "_PulseAmount", "_PulseSpeed");

        // Noise
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_NOISE", "_EnableNoise", "Noise Effect",
            "_NoiseAmount", "_NoiseScale", "_NoiseSpeed");

        // Wind
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_WIND", "_EnableWind", "Wind Effect",
            "_WindDirection", "_WindSpeed", "_WindStrength", "_WindTurbulence");

        // Bend
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_BEND", "_EnableBend", "Bend Effect",
            "_BendAmount", "_BendDirection");

        // Melt
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_MELT", "_EnableMelt", "Melt Effect",
            "_MeltHeight", "_MeltRange", "_MeltSpeed");

        // Ripple
        DrawEffectGroup(materialEditor, properties, material, "EFFECT_RIPPLE", "_EnableRipple", "Ripple Effect",
            "_RippleCenter", "_RippleAmount", "_RippleSpeed", "_RippleFrequency");

        EditorGUILayout.Space(20);
        GUILayout.Label("Lighting", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_AmbientBoost");
    }

    void DrawEffectGroup(MaterialEditor materialEditor, MaterialProperty[] properties, Material material, 
        string keyword, string toggleProp, string label, params string[] propNames)
    {
        EditorGUILayout.Space(10);
        
        var toggle = FindProperty(toggleProp, properties);
        bool isEnabled = toggle.floatValue > 0.5f;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUI.BeginChangeCheck();
        isEnabled = EditorGUILayout.Toggle(label, isEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            toggle.floatValue = isEnabled ? 1.0f : 0.0f;
            
            if (isEnabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        if (isEnabled)
        {
            EditorGUI.indentLevel++;
            foreach (string propName in propNames)
            {
                DrawProperty(materialEditor, properties, propName);
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }

    void DrawProperty(MaterialEditor materialEditor, MaterialProperty[] properties, string name)
    {
        var prop = FindProperty(name, properties, false);
        if (prop != null)
        {
            materialEditor.ShaderProperty(prop, prop.displayName);
        }
    }
}