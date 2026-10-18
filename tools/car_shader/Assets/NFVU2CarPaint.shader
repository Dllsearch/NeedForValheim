Shader "NFVU2/CarPaint"
{
    Properties
    {
        _Color ("Body colour", Color) = (1,1,1,1)
        _MainTex ("Body texture and vinyl", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.3
        _Glossiness ("Smoothness", Range(0,1)) = 0.65
        _FinishMode ("Finish: ordinary / pearl / chameleon", Range(0,2)) = 0
        _PearlColor ("Pearl highlight tint", Color) = (0.8,0.9,1,1)
        _ShiftColor ("Chameleon grazing colour", Color) = (0.4,0.1,0.8,1)
        _ShiftStrength ("Colour shift strength", Range(0,1)) = 0.65
        _ShiftPower ("Colour shift falloff", Range(0.25,8)) = 2
        _CompositePaint ("Layered vinyl paint mask", Float) = 0
        _PaintBaseColor ("Layered vinyl foundation", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull Back
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color, _PearlColor, _ShiftColor;
        half _Metallic, _Glossiness, _FinishMode, _ShiftStrength, _ShiftPower;
        half _CompositePaint;
        fixed4 _PaintBaseColor;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 sampled = tex2D(_MainTex, IN.uv_MainTex);
            half facing = saturate(abs(dot(normalize(IN.worldNormal), normalize(UnityWorldSpaceViewDir(IN.worldPos)))));
            half grazing = pow(1 - facing, _ShiftPower);
            half3 foundation = _CompositePaint > 0.5 ? _PaintBaseColor.rgb : _Color.rgb;
            half3 tint = foundation;
            if (_FinishMode > 1.5)
                tint = lerp(tint, _ShiftColor.rgb, grazing * _ShiftStrength);
            else if (_FinishMode > 0.5)
                tint = lerp(tint, _PearlColor.rgb, grazing * _ShiftStrength * 0.35);
            // Composite RGB already contains the base paint. Shift only its
            // remaining contribution; artwork coverage is stored in alpha.
            o.Albedo = _CompositePaint > 0.5
                ? max(0, sampled.rgb + sampled.a * (tint - foundation))
                : sampled.rgb * tint;
            o.Metallic = saturate(_Metallic);
            o.Smoothness = saturate(_Glossiness);
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
