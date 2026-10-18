// Custom shader wrote originally for Incrate
// Followed this tutorial: https://www.youtube.com/watch?v=CJKGIXJjqlg

shader_type canvas_item;

varying float time;
const float scale = 5.0;

vec3 plasma(vec2 pos)
{
	pos = pos * scale - scale / 1.0;
	
	float wave1 = sin(pos.x + time) / 2.0;
	float wave2 = sin(pos.y + time * 2.0) + 0.5;
	float wave3 = sin(pos.x + pos.y + time * 0.5);
	
	float ring = sin(sqrt(pos.x * pos.x + pos.y * pos.y) + time);
	
	float finalWave = (wave1 + wave2 + wave3 + ring) * 0.2;
	return vec3(0.03 + sin(finalWave * 0.2 * PI) * 0.2, 0.06 - cos(finalWave * PI) * 0.25, sin(finalWave / PI) * 2.8);
} 

void fragment() {
	time = TIME * 0.05f;
	
    COLOR = vec4(plasma(UV), 1);
}
