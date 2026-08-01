// WaterShimmer.fx - Pixel Shader 3.0 for subtle water shimmer effect
// Inspired by The Legend of Zelda: The Wind Waker

sampler2D implicitInput : register(s0);
sampler2D noiseMap       : register(s1);
float time              : register(c0); // Passed from C# Animation

float4 main(float2 uv : TEXCOORD) : COLOR 
{
	// Scroll two noise texture UVs at different speeds for non-repetitive waves
	float2 speed1 = float2(time * 0.03, time * 0.02);
	float2 speed2 = float2(time * -0.02, time * 0.04);

	// Sample noise and calculate ripple distortion offset
	float distortion1 = tex2D(noiseMap, uv + speed1).r;
	float distortion2 = tex2D(noiseMap, uv * 1.5 + speed2).g;

	// Combine noise maps for non-repetitive wave patterns
	float2 finalOffset = (float2(distortion1, distortion2) - 0.5) * 0.015; // Kept subtle

	// Distort main image texture UVs
	float4 color = tex2D(implicitInput, uv + finalOffset);

	// Add a tiny specular light shimmer on wave peaks
	float shimmer = pow(distortion1 * distortion2, 3.0) * 0.2;
	color.rgb += float3(shimmer, shimmer, shimmer);

	return color;
}
