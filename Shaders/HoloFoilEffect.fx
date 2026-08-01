// Sampler Registers (Textures)
sampler2D implicitInput  : register(s0); // Card Artwork (WPF Control)
sampler2D rainbowTex     : register(s1); // Rainbow Spectrum Image

// Constant Registers (Data passed from C#)
float2 TiltAngle         : register(c0); // Mouse Tilt Offset (X, Y)
float Time               : register(c1); // Animated time for pulsing effect

float4 main(float2 uv : TEXCOORD) : COLOR
{
	// Sample the base card artwork
	float4 baseColor = tex2D(implicitInput, uv);

	// Calculate edge distance from card borders (0.0 at edges, 1.0 at center)
	float2 edgeDist = abs(uv - 0.5) * 2.0;
	float edgeMask = max(edgeDist.x, edgeDist.y);

	// Create edge highlight region (only outer 15% of card)
	float edgeIntensity = smoothstep(0.70, 1.0, edgeMask);

	// Calculate Rainbow Gradient Shift based on UV, Tilt, and Time
	float rainbowCoord = uv.x * 0.7 + uv.y * 0.3 
						+ (TiltAngle.x * 0.8) 
						+ (TiltAngle.y * 0.4)
						+ (Time * 0.05); // Slow rainbow movement over time

	// Wrap texture coordinate between 0.0 and 1.0
	rainbowCoord = frac(rainbowCoord);

	float4 rainbowColor = tex2D(rainbowTex, float2(rainbowCoord, 0.5));

	// Calculate Specular Glare (Sharp White Light Streak along diagonal)
	float streakPosition = (TiltAngle.x + TiltAngle.y) * 0.5 + 0.5;
	float distToStreak = abs((uv.x + uv.y) * 0.5 - streakPosition);

	// Create sharp specular highlight
	float glare = pow(max(0.0, 1.0 - distToStreak * 3.0), 16.0);

	// Blend rainbow foil on EDGES only with subtle glare
	float3 edgeFoil = baseColor.rgb * (1.0 + rainbowColor.rgb * edgeIntensity * 1.5);
	float3 finalColor = edgeFoil + (float3(glare, glare, glare) * edgeIntensity * 0.8);

	return float4(finalColor, baseColor.a);
}
