Shader "Hidden/GradientBackground"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off

		Pass
		{
			CGPROGRAM
			#include "GradientBackgroundCore.cginc"
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile VERTICAL_GRADIENT HORIZONTAL_GRADIENT RADIAL_FIT_GRADIENT RADIAL_ASPECT_WIDTH_GRADIENT RADIAL_ASPECT_HEIGHT_GRADIENT
			
			#include "UnityCG.cginc"
			
			#define MAXIMUM_COLORS  6
			sampler2D _MainTex;
			fixed3 _GradientColors[MAXIMUM_COLORS];
			float _GradientTimes[MAXIMUM_COLORS];
			float _GradientColorsSize;
			float _ScreenRatioWidthDividedByHeight;
			float _ScreenRatioHeightDividedByWidth;
			float _InvertDirection;
			float2 _GradientOrigin;
			
			float calcHorizontalGradientNormalizedTime(float2 uv) {
			    return uv.x;
			}
			
			float calcVerticalGradientNormalizedTime(float2 uv) {
			    return uv.y;
			}
			
			float calcRadialGradientNormalizedTime(float2 uv) {
			    uv -= _GradientOrigin;
                float distanceSquared = uv.x * uv.x + uv.y * uv.y;
                return min(distanceSquared * 4,1.0);
			}
			
			float calcAspectCorrectFitWidthRadialGradientNormalizedTime(float2 uv) {
			    uv -= _GradientOrigin;
			    uv.y *= _ScreenRatioHeightDividedByWidth;
			    float distanceSquared = uv.x * uv.x + uv.y * uv.y;
                return min(distanceSquared * 4,1.0);
            }
            
            float calcAspectCorrectFitHeightRadialGradientNormalizedTime(float2 uv) {
                uv -= _GradientOrigin;
                uv.x *= _ScreenRatioWidthDividedByHeight;
                float distanceSquared = uv.x * uv.x + uv.y * uv.y;
                return min(distanceSquared * 4,1.0);
            }
			
			fixed3 calcColorForNormalizedTime(float t) {
			
			    int currentIndex = 0;
			    int endIndex = 999;
			    
			    for(;currentIndex < MAXIMUM_COLORS; currentIndex ++) {
			    
                    float currentTime = _GradientTimes[currentIndex];
                    float diff = currentTime - t;
                    float signal = step(0.0,diff);
                    float indexToConsider = lerp(endIndex,currentIndex,signal); 
                    endIndex = min(endIndex,indexToConsider);

			    }
			    
			    int startIndex = max(endIndex-1,0);
			    
			    fixed3 startColor = _GradientColors[startIndex];
			    fixed3 endColor = _GradientColors[endIndex];
			    
			    float startTime = _GradientTimes[startIndex];
			    float endTime = _GradientTimes[endIndex];

            float lerpFactor = (t - startTime) / (max((endTime - startTime),0.001));

            lerpFactor = saturate(lerpFactor);

            return lerp(startColor,endColor,lerpFactor);
			}
			
			float calcNormalizedTime(float2 uv) {
			    #ifdef VERTICAL_GRADIENT
			    return calcVerticalGradientNormalizedTime(uv);
			    #elif HORIZONTAL_GRADIENT
			    return calcHorizontalGradientNormalizedTime(uv);
			    #elif RADIAL_FIT_GRADIENT
			    return calcRadialGradientNormalizedTime(uv);
			    #elif RADIAL_ASPECT_HEIGHT_GRADIENT
			    return calcAspectCorrectFitHeightRadialGradientNormalizedTime(uv);
			    #elif RADIAL_ASPECT_WIDTH_GRADIENT
			    return calcAspectCorrectFitWidthRadialGradientNormalizedTime(uv);
			    #else
			    return 0.0;
			    #endif
			}

			fixed4 frag (v2f i) : SV_Target
			{
			    float t = calcNormalizedTime(i.uv);
			    if(_InvertDirection == 1.0) t = 1.0 - t; //This if isn't problematic because it will produce the same result for the entire program execution, since we are checking for an uniform value
			    fixed3 color =  calcColorForNormalizedTime(t);
			    
				return fixed4(color,1.0);
			}
			ENDCG
		}
		
		Pass
        {
            CGPROGRAM
			#include "GradientBackgroundCore.cginc"
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            
            fixed3 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex,i.uv).rgb;
            }
            ENDCG
        }
	}
}
