// Adapted from Godot Shaders Dot Matrix muzi1983
// https://godotshaders.com/shader/dot-matrix-diagonal-reveal-shader/
shader_type canvas_item;

uniform float cell_size = 32.0;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 dot_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform bool inverted = false;

void fragment() {
	if (inverted && progress <= 0.0)
	{
		COLOR = dot_color;
	}
	else if (progress <= 0.0) discard;
	else if (progress < 1.0) 
	{	
	    // Split up the screen into a grid
	    vec2 screen_size = vec2(1.0) / SCREEN_PIXEL_SIZE;
	    vec2 cell_count = floor(screen_size / cell_size);

	    // find out where middle of cells are
	    vec2 aligned_pos = UV * screen_size / (cell_count * cell_size);

	    // offset the cell to get its center
	    float centered = (aligned_pos.x + aligned_pos.y) * 0.6;

	    // window of the effect around the diagonal
	    float transition = 0.25;
	    float scale = smoothstep(centered - transition, centered + transition, progress * (1.0 + transition));
		
		// clear small dots
		if (!inverted && scale < 0.01) discard;

		// dot matrix
	    vec2 screen_cells = (floor(UV * screen_size / cell_size) + 0.5) * cell_size;
	    float from_cell_center = length(UV * screen_size - screen_cells);
	    float radius = scale * cell_size * 0.8;

		if (inverted)
		{
			float alpha = smoothstep(radius + 1.5, max(radius - 1.5, 0.0), from_cell_center);
			if (inverted && scale < 0.01) alpha = 0.0;
		    COLOR = vec4(dot_color.rgb, 1.0 - alpha);
		}
		else
		{
		    float alpha = 1.0 - smoothstep(max(radius - 1.5, 0.0), radius + 1.5, from_cell_center);
		    COLOR = vec4(dot_color.rgb, alpha);
		}
	}
	else if (!inverted)
	{
		COLOR = dot_color;
	}
	else
	{
		discard;
	}
}
