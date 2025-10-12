//lpm_compare CBX_SINGLE_OUTPUT_FILE="ON" LPM_HINT="ONE_INPUT_IS_CONSTANT=YES" LPM_REPRESENTATION="UNSIGNED" LPM_TYPE="LPM_COMPARE" LPM_WIDTH=6 agb dataa datab
//VERSION_BEGIN 24.1 cbx_mgl 2025:03:05:20:07:01:SC cbx_stratixii 2025:03:05:20:06:36:SC cbx_util_mgl 2025:03:05:20:06:36:SC  VERSION_END
// synthesis VERILOG_INPUT_VERSION VERILOG_2001
// altera message_off 10463



// Copyright (C) 2025  Altera Corporation. All rights reserved.
//  Your use of Altera Corporation's design tools, logic functions 
//  and other software and tools, and any partner logic 
//  functions, and any output files from any of the foregoing 
//  (including device programming or simulation files), and any 
//  associated documentation or information are expressly subject 
//  to the terms and conditions of the Altera Program License 
//  Subscription Agreement, the Altera Quartus Prime License Agreement,
//  the Altera IP License Agreement, or other applicable license
//  agreement, including, without limitation, that your use is for
//  the sole purpose of programming logic devices manufactured by
//  Altera and sold by Altera or its authorized distributors.  Please
//  refer to the Altera Software License Subscription Agreements 
//  on the Quartus Prime software download page.



//synthesis_resources = lpm_compare 1 
//synopsys translate_off
`timescale 1 ps / 1 ps
//synopsys translate_on
module  mg8te
	( 
	agb,
	dataa,
	datab) /* synthesis synthesis_clearbox=1 */;
	output   agb;
	input   [5:0]  dataa;
	input   [5:0]  datab;

	wire  wire_mgl_prim1_agb;

	lpm_compare   mgl_prim1
	( 
	.agb(wire_mgl_prim1_agb),
	.dataa(dataa),
	.datab(datab));
	defparam
		mgl_prim1.lpm_representation = "UNSIGNED",
		mgl_prim1.lpm_type = "LPM_COMPARE",
		mgl_prim1.lpm_width = 6,
		mgl_prim1.lpm_hint = "ONE_INPUT_IS_CONSTANT=YES";
	assign
		agb = wire_mgl_prim1_agb;
endmodule //mg8te
//VALID FILE
