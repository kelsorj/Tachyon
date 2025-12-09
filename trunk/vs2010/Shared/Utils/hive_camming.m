# provides functions for calculating the necessary Z and theta
# moves for a given Y, and cam table generation with a given
# resolution.

1; # hack to make Octave load more than one function

# given a start Y and end Y, determine the Z camming move
# y_end is the teachpoint Y pos
# y_start is where the robot is starting the camming
function z_camming = GetZCammingRange( y_start, y_end)
	arm_length = 250;
	z_start = sqrt( arm_length * arm_length - y_start * y_start);
	z_end = sqrt( arm_length * arm_length - y_end * y_end);
	z_camming = z_start - z_end;
endfunction

function z_world = ConvertZToolToWorld( z_tool, y_tool)
	arm_length = 250;
	finger_offset = 28;
	z_world = finger_offset + z_tool + sqrt( arm_length * arm_length - y_tool * y_tool)
endfunction

function y = GetYFromTheta( theta)
	arm_length = 250;
	y = arm_length * sind( theta);
endfunction

function [y_tool,z_tool] = ConvertTZWorldToYZTool( t_world, z_world)
	arm_length = 250;
	finger_offset = 28;
	y_tool = GetYFromTheta( t_world);
	z_for_y = sqrt( arm_length * arm_length - y_tool * y_tool);
	z_tool = z_world - finger_offset - z_for_y;
endfunction

ConvertZToolToWorld( 10, 95)
GetZCammingRange( 9, 95)
GetYFromTheta( 5.3)
[y_tool,z_tool] = ConvertTZWorldToYZTool( 25, 245)
