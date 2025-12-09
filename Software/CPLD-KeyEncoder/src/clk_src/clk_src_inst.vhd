	component clk_src is
		port (
			clk : out std_logic   -- clk
		);
	end component clk_src;

	u0 : component clk_src
		port map (
			clk => CONNECTED_TO_clk  -- clk.clk
		);

