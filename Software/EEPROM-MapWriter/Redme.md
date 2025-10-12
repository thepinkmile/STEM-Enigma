# Map Writer - Rotor Encoder

This directory contains the code to enable writing encoder maps to the EEPROM of a Rotor.
The rotor accepts 6-bit binary number and includes a rotor "shifter" to enable multiple maps to be configured and used.

This application/software will be deployed on a Raspberry Pi CM5 (or later), and will be connected to the rotor viat the GPIO pins using Pogo Pin connectors.
